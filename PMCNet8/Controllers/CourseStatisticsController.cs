using Data.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;
using PMCNet8.Models;
using System.Linq;

public class CourseStatisticsController : Controller
{
    private readonly Medihub4rumDbContext _mediHub4RumContext;
    private readonly ILogger<CourseStatisticsController> _logger;
    private readonly MedihubSCAppDbContext _mediHubSCAppContext;
    private readonly LogActionDbContext _logActionDbContext;
    public CourseStatisticsController( LogActionDbContext logActionDbContext  ,Medihub4rumDbContext mediHub4RumContext, ILogger<CourseStatisticsController> logger, MedihubSCAppDbContext mediHubSCAppContext)
    {

        _mediHub4RumContext = mediHub4RumContext;
        _logger = logger;
        _mediHubSCAppContext = mediHubSCAppContext;
        _logActionDbContext = logActionDbContext;
    }

    public async Task<IActionResult> Index(Guid? courseId)
    {
        var model = new CourseStatisticViewModel();

        if (!Guid.TryParse(HttpContext.Session.GetString("SponsorId"), out Guid sponsorId))
        {
            return BadRequest("Invalid SponsorId");
        }

        model.CourseListItems = await GetCoursesAsync(sponsorId);

        if (courseId.HasValue)
        {
            model.SelectedCourseId = courseId.Value;
            model.CourseInfo = await GetCourseInfoAsync(courseId.Value);
            model.UnregisteredPharmacists = (await GetUnregisteredPharmacistAsync(courseId.Value)).ToList();
            model.AchieveTargets = await GetAchieveTargetsAsync(courseId.Value);
        }
        if (model.AchieveTargets == null)
        {
            model.AchieveTargets = new AchieveTargetsViewModel();
        }
        if (model.CourseInfo == null)
        {
            model.CourseInfo = new GetCourseViewModel();
        }
        if (model.UnregisteredPharmacists == null)
        {
            model.UnregisteredPharmacists = new List<UnregisteredPharmacistViewModel>();
        }
        else if (model.CourseListItems.Any())
        {
            model.SelectedCourseId = model.CourseListItems.First().Id;
            model.CourseInfo = await GetCourseInfoAsync(model.SelectedCourseId);
            model.UnregisteredPharmacists = (await GetUnregisteredPharmacistAsync(model.SelectedCourseId)).ToList();
        }

        return View(model);
    }


    [HttpGet("api/courses")]
    public async Task<IActionResult> GetCourses()
    {
        if (!Guid.TryParse(HttpContext.Session.GetString("SponsorId"), out Guid sponsorId))
        {
            return BadRequest("Invalid SponsorId");
        }
        var courses = await GetCoursesAsync(sponsorId);
        return Ok(courses);    
    }

    [HttpGet("api/course/{courseId}")]
    public async Task<IActionResult> GetCourseInfo(Guid courseId)
    {
        var courseInfo = await GetCourseInfoAsync(courseId);
        return Ok(courseInfo);
    }

    private async Task<List<CourseListItems>> GetCoursesAsync(Guid sponsorId)
    {
        return await _mediHub4RumContext.SponsorHubCourse
            .Where(shc => shc.SponsorId == sponsorId)
            .Select(shc => new CourseListItems
            {
                Id = shc.CategoryId,
                Name = shc.Category.Name
            })
            .ToListAsync();
    }

    private async Task<GetCourseViewModel> GetCourseInfoAsync(Guid courseId)
    {
        try
        {
            var courseName = await _mediHub4RumContext.Category
                .Where(c => c.Id == courseId)
                .Select(c => c.Name)
                .FirstOrDefaultAsync();

            var topics = await _mediHub4RumContext.Topic
                .Where(t => t.Category_Id == courseId)
                .Select(t => new TopicInfo { Id = t.Id, Name = t.Name })
                .ToListAsync();

            var courseInfo = new GetCourseViewModel();
            courseInfo.Name = courseName ?? "Không có tên";
            courseInfo.Description = await _mediHub4RumContext.Category
                    .Where(c => c.Id == courseId)
                    .Select(c => c.Description ?? "Không có mô tả")
                    .FirstOrDefaultAsync();
            courseInfo.Type = await _mediHub4RumContext.SponsorHubCourse
                    .Where(c => c.CategoryId == courseId)
                    .Select(c => c.CourseType)
                    .FirstOrDefaultAsync();
            courseInfo.Quantity = topics.Count;

            // Log thông tin để debug
            _logger.LogInformation($"CourseId: {courseId}, Name: {courseInfo.Name}, TopicCount: {courseInfo.Quantity}");

            // Lấy thông tin về các keys của khóa học
            var keyStats = await GetKeyCourseActiveStatsAsync(courseId);
            courseInfo.TotalKeys = keyStats.TotalKeys;
            courseInfo.UsedKeys = keyStats.UsedKeys;
            courseInfo.UnusedKeys = keyStats.UnusedKeys;
            courseInfo.PieData = new object[]
            {
            new object[] { "Status", "Count" },
            new object[] { "Đã sử dụng", keyStats.UsedKeys },
            new object[] { "Chưa sử dụng", keyStats.UnusedKeys }
            };

            return courseInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching course info for courseId: {CourseId}", courseId);
            return new GetCourseViewModel { Name = "Error", Description = "Không thể tải thông tin khóa học" };
        }
    }

    private async Task<(int TotalKeys, int UsedKeys, int UnusedKeys)> GetKeyCourseActiveStatsAsync(Guid courseId)
    {
        var stats = await _mediHubSCAppContext.CPECourseActive
            .Where(cpa => cpa.CategoryId == courseId)
            .GroupBy(cpa => cpa.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        int totalKeys = stats.Sum(s => s.Count);
        int usedKeys = stats.FirstOrDefault(s => s.Status == "A")?.Count ?? 0;
        int unusedKeys = totalKeys - usedKeys;

        return (totalKeys, usedKeys, unusedKeys);
    }


    private async Task<IEnumerable<UnregisteredPharmacistViewModel>> GetUnregisteredPharmacistAsync(Guid courseId)
    {
        try
        {
            var courseActiveKeys = await _mediHubSCAppContext.CPECourseActive
                .Where(cpa => cpa.CategoryId == courseId)
                .Select(cpa => cpa.KeyCodeActive)
                .ToListAsync();

            var appSetups = await _mediHubSCAppContext.AppSetup
                .Where(app => courseActiveKeys.Contains(app.KeyCodeActive))
                .Select(app => new
                {
                    app.KeyCodeActive,
                    app.SCName,
                    app.PhoneNumber,
                    app.DrugName,
                    app.Address
                })
                .ToListAsync();

            var registeredKeys = await _mediHub4RumContext.MediHubScQuizResult
                .Select(qr => qr.KeyAppActive)
                .ToListAsync();

            var unregisteredPharmacists = appSetups
        .Where(setup => setup.KeyCodeActive != null && !registeredKeys.Contains(setup.KeyCodeActive))
        .Select(setup => new UnregisteredPharmacistViewModel
        {
            TenDuocSi = setup.SCName ?? "",
            SoDienThoai = setup.PhoneNumber ?? "",
            DonViCongTac = setup.DrugName ?? "",
            DiaChi = setup.Address ?? "",
            DaXacThuc = false,
            KeyCodeActive = setup.KeyCodeActive
        })
        .ToList();

            var userIds = await _mediHub4RumContext.MembershipUser
                .Where(mu => unregisteredPharmacists.Select(up => up.KeyCodeActive).Contains(mu.KeyCodeActive))
                .Select(mu => new { mu.Id, mu.KeyCodeActive })
                .ToListAsync();

            var userInfos = await _mediHub4RumContext.MembershipUserInfo
            .Where(mui => userIds.Select(u => u.Id).Contains(mui.UserId))
            .Select(mui => new { mui.UserId, mui.TrangThai })
            .ToListAsync();

            foreach (var pharmacist in unregisteredPharmacists)
            {
                var userId = userIds.FirstOrDefault(u => u.KeyCodeActive == pharmacist.KeyCodeActive)?.Id;
                if (userId != null)
                {
                    var userInfo = userInfos.FirstOrDefault(ui => ui.UserId == userId);
                    pharmacist.DaXacThuc = userInfo?.TrangThai == 2;
                }
                else
                {
                    pharmacist.DaXacThuc = false;
                }
            }

            return unregisteredPharmacists;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred in GetUnregisteredPharmacistAsync");
            throw;
        }
    }

    [HttpGet("api/unregistered-pharmacists/{courseId}")]
    public async Task<IActionResult> GetUnregisteredPharmacists(Guid courseId)
    {
        try
        {
            var result = await GetUnregisteredPharmacistAsync(courseId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error occurred while processing request for courseId: {courseId}");
            return StatusCode(500, $"An error occurred: {ex.Message}. Please check the server logs for more details.");
        }
    }

    //private async Task<AchieveTargetsViewModel> GetAchieveTargetsAsync(Guid courseId)
    //{
    //    var check = await _logActionDbContext.LogClickCourse
    //            .AsNoTracking()
    //            .AnyAsync(e => e.CourseId == courseId);
    //    if (!check) return new AchieveTargetsViewModel();

    //    try
    //    {
    //        var timeAndTarget = await _mediHub4RumContext.SponsorHubCourse
    //            .Where(e => e.CategoryId == courseId)
    //            .Select(e => new AchieveTargetsViewModel
    //            {
    //                TargetStartDate = e.TargetStartDate,
    //                TargetEndDate = e.TargetEndDate,
    //                TargetFinish = e.TargetFinish,
    //                TargetJoin = e.TargetJoin,
    //            })
    //            .FirstOrDefaultAsync();

    //        if (timeAndTarget == null) timeAndTarget = new AchieveTargetsViewModel();

    //        var day1 = timeAndTarget.TargetStartDate = await _logActionDbContext.LogClickCourse
    //            .AsNoTracking()
    //            .Where(e => e.CourseId == courseId)
    //            .MinAsync(e => e.CreatedDate.Date);

    //        var day5 = timeAndTarget.TargetEndDate = await _logActionDbContext.LogClickCourse
    //            .AsNoTracking()
    //            .Where(e => e.CourseId == courseId)
    //            .MaxAsync(e => e.CreatedDate.Date);
    //        day5 = day5.Value.AddDays(1).AddMilliseconds(-1);


    //        timeAndTarget.TotalJoins = new Dictionary<DateTime, int>();
    //        timeAndTarget.TotalJoins[day1.Value] = 0;
    //        timeAndTarget.TotalJoins[day5.Value] = await _logActionDbContext.LogClickCourse.AsNoTracking().Where(e => e.CourseId == courseId && day1 <= e.CreatedDate && e.CreatedDate < day5).Select(e => e.UserId).Distinct().CountAsync();

    //        timeAndTarget.TotalFinishs = new Dictionary<DateTime, int>();
    //        timeAndTarget.TotalFinishs[day1.Value] = 0;
    //        timeAndTarget.TotalFinishs[day5.Value] = await _mediHub4RumContext.SponsorHubCourseFinish.AsNoTracking().Where(e => e.CategoryId == courseId && day1 <= e.FinishDate && e.FinishDate < day5).Select(e => e.UserId).Distinct().CountAsync();

    //        if (timeAndTarget.TargetStartDate.Value != timeAndTarget.TargetEndDate.Value)
    //        {
    //            var totalDays = (timeAndTarget.TargetEndDate - timeAndTarget.TargetStartDate).Value.Days;
    //            var middle1 = totalDays / 2;
    //            var middle2 = middle1 / 2;

    //            var day2 = timeAndTarget.TargetStartDate.Value.AddDays(middle2);
    //            var day3 = timeAndTarget.TargetStartDate.Value.AddDays(middle1);
    //            var day4 = timeAndTarget.TargetStartDate.Value.AddDays(middle1 + middle2);

    //            timeAndTarget.TotalJoins[day2] = await _logActionDbContext.LogClickCourse.AsNoTracking().Where(e => e.CourseId == courseId && day1 <= e.CreatedDate && e.CreatedDate < day2).Select(e => e.UserId).Distinct().CountAsync();
    //            timeAndTarget.TotalJoins[day3] = await _logActionDbContext.LogClickCourse.AsNoTracking().Where(e => e.CourseId == courseId && day1 <= e.CreatedDate && e.CreatedDate < day3).Select(e => e.UserId).Distinct().CountAsync();
    //            timeAndTarget.TotalJoins[day4] = await _logActionDbContext.LogClickCourse.AsNoTracking().Where(e => e.CourseId == courseId && day1 <= e.CreatedDate && e.CreatedDate < day4).Select(e => e.UserId).Distinct().CountAsync();

    //            timeAndTarget.TotalFinishs[day2] = await _mediHub4RumContext.SponsorHubCourseFinish.AsNoTracking().Where(e => e.CategoryId == courseId && day1 <= e.FinishDate && e.FinishDate < day2).Select(e => e.UserId).Distinct().CountAsync();
    //            timeAndTarget.TotalFinishs[day3] = await _mediHub4RumContext.SponsorHubCourseFinish.AsNoTracking().Where(e => e.CategoryId == courseId && day1 <= e.FinishDate && e.FinishDate < day3).Select(e => e.UserId).Distinct().CountAsync();
    //            timeAndTarget.TotalFinishs[day4] = await _mediHub4RumContext.SponsorHubCourseFinish.AsNoTracking().Where(e => e.CategoryId == courseId && day1 <= e.FinishDate && e.FinishDate < day4).Select(e => e.UserId).Distinct().CountAsync();
    //        }

    //        return timeAndTarget;
    //    }
    //    catch (SqlException sqlEx)
    //    {
    //        _logger.LogError(sqlEx, "SQL error occurred in GetAchieveTargetsAsync for courseId: {CourseId}", courseId);
    //        throw new ApplicationException("An error occurred while retrieving data from the database.", sqlEx);
    //    }
    //    catch (Exception ex)
    //    {
    //        _logger.LogError(ex, "Unexpected error occurred in GetAchieveTargetsAsync for courseId: {CourseId}", courseId);
    //        throw new ApplicationException("An unexpected error occurred while processing your request.", ex);
    //    }
    //}
    private async Task<AchieveTargetsViewModel> GetAchieveTargetsAsync(Guid courseId)
    {
        var result = new AchieveTargetsViewModel();

        try
        {
            // Lấy thông tin từ SponsorHubCourse
            var courseInfo = await _mediHub4RumContext.SponsorHubCourse
                .Where(e => e.CategoryId == courseId)
                .Select(e => new
                {
                    e.TargetStartDate,
                    e.TargetEndDate,
                    e.TargetFinish,
                    e.TargetJoin
                })
                .FirstOrDefaultAsync();

            if (courseInfo != null)
            {
                result.TargetStartDate = courseInfo.TargetStartDate;
                result.TargetEndDate = courseInfo.TargetEndDate;
                result.TargetFinish = courseInfo.TargetFinish;
                result.TargetJoin = courseInfo.TargetJoin;
            }

            // Nếu không có dữ liệu trong SponsorHubCourse, lấy từ LogClickCourse
            if (!result.TargetStartDate.HasValue || !result.TargetEndDate.HasValue)
            {
                var logDates = await _logActionDbContext.LogClickCourse
                    .Where(e => e.CourseId == courseId)
                    .Select(e => e.CreatedDate)
                    .ToListAsync();

                if (logDates.Any())
                {
                    result.TargetStartDate = logDates.Min().Date;
                    result.TargetEndDate = logDates.Max().Date.AddDays(1).AddMilliseconds(-1);
                }
                else
                {
                    // Nếu không có dữ liệu, sử dụng ngày hiện tại
                    result.TargetStartDate = DateTime.Now.Date;
                    result.TargetEndDate = DateTime.Now.Date.AddDays(30);
                }
            }

            // Tính toán 5 mốc thời gian
            var timePoints = CalculateTimePoints(result.TargetStartDate.Value, result.TargetEndDate.Value);

            result.TotalJoins = new Dictionary<DateTime, int>();
            result.TotalFinishs = new Dictionary<DateTime, int>();

            foreach (var point in timePoints)
            {
                // Tính tổng số người tham gia đến thời điểm này
                result.TotalJoins[point] = await _logActionDbContext.LogClickCourse
                    .Where(e => e.CourseId == courseId && e.CreatedDate < point)
                    .Select(e => e.UserId)
                    .Distinct()
                    .CountAsync();

                // Tính tổng số người hoàn thành đến thời điểm này
                result.TotalFinishs[point] = await _mediHub4RumContext.SponsorHubCourseFinish
                    .Where(e => e.CategoryId == courseId && e.FinishDate < point)
                    .Select(e => e.UserId)
                    .Distinct()
                    .CountAsync();
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetAchieveTargetsAsync for courseId: {CourseId}", courseId);
            throw;
        }
    }

    private List<DateTime> CalculateTimePoints(DateTime start, DateTime end)
    {
        var points = new List<DateTime> { start };
        var totalDays = (end - start).Days;

        for (int i = 1; i <= 3; i++)
        {
            points.Add(start.AddDays(totalDays * i / 4));
        }

        points.Add(end);
        return points;
    }

    [HttpGet("api/achieve-targets/{courseId}")]
    public async Task<IActionResult> GetAchieveTargets(Guid courseId)
    {
        try
        {
            var result = await GetAchieveTargetsAsync(courseId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error occurred while processing request for courseId: {courseId}");
            return StatusCode(500, $"An error occurred: {ex.Message}. Please check the server logs for more details.");
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetCourseStatistics(Guid courseId)
    {
        var model = new CourseStatisticViewModel();

        model.SelectedCourseId = courseId;
        model.CourseInfo = await GetCourseInfoAsync(courseId);
        model.UnregisteredPharmacists = (await GetUnregisteredPharmacistAsync(courseId)).ToList();
        model.AchieveTargets = await GetAchieveTargetsAsync(courseId);

        return PartialView("_CourseStatistics", model);
    }

}
