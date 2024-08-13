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

    private async Task<AchieveTargetsViewModel> GetAchieveTargetsAsync(Guid courseId)
    {
        var from = DateTime.Now.Date;
        var to = from.AddDays(1);
        try
        {
            var timeAndTarget = await _mediHub4RumContext.SponsorHubCourse
                .Where(e => e.CategoryId == courseId)
                .Select(e => new AchieveTargetsViewModel
                {
                    TargetStartDate = e.TargetStartDate,
                    TargetEndDate = e.TargetEndDate,
                    TargetFinish = e.TargetFinish,
                    TargetJoin = e.TargetJoin,
                })
                .FirstOrDefaultAsync();

            if (timeAndTarget == null)
            {
                timeAndTarget = new AchieveTargetsViewModel();
            }

            // Set default values if dates are null
            timeAndTarget.TargetStartDate ??= DateTime.MinValue;
            timeAndTarget.TargetEndDate ??= DateTime.MinValue;

            // Calculate statistics
            timeAndTarget.TotalBeforeNow = await _logActionDbContext.LogClickCourse
                .AsNoTracking()
                .Where(e => e.CourseId == courseId)
                .Select(e =>e.UserId)
                .Distinct()
                .CountAsync();

            timeAndTarget.TotalDay = await _logActionDbContext.LogClickCourse
                .AsNoTracking()
                .Where(e => e.CourseId == courseId && e.CreatedDate >= from && e.CreatedDate < to)
                .Distinct()
                .CountAsync();

            timeAndTarget.FinishBeforeNow = await _mediHub4RumContext.SponsorHubCourseFinish
                .AsNoTracking()
                .Where(e => e.CategoryId == courseId)
                .Select(e => e.UserId)
                .Distinct()
                .CountAsync();

            timeAndTarget.FinishDay = await _mediHub4RumContext.SponsorHubCourseFinish
                .AsNoTracking()
                .Where(e => e.CategoryId == courseId && e.FinishDate >= from && e.FinishDate < to)
                .Select (e =>e.UserId)
                .Distinct()
                .CountAsync();

            return timeAndTarget;
        }
        catch (SqlException sqlEx)
        {
            _logger.LogError(sqlEx, "SQL error occurred in GetAchieveTargetsAsync for courseId: {CourseId}", courseId);
            throw new ApplicationException("An error occurred while retrieving data from the database.", sqlEx);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred in GetAchieveTargetsAsync for courseId: {CourseId}", courseId);
            throw new ApplicationException("An unexpected error occurred while processing your request.", ex);
        }
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
