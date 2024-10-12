using Data.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PMCNet8.Models;

public class CourseStatisticsController : Controller
{
    private readonly Medihub4rumDbContext _mediHub4RumContext;
    private readonly ILogger<CourseStatisticsController> _logger;
    private readonly MedihubSCAppDbContext _mediHubSCAppContext;
    private readonly LogActionDbContext _logActionDbContext;
    public CourseStatisticsController(LogActionDbContext logActionDbContext, Medihub4rumDbContext mediHub4RumContext, ILogger<CourseStatisticsController> logger, MedihubSCAppDbContext mediHubSCAppContext)
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
            //return BadRequest("Invalid SponsorId");
            return RedirectToAction("Login", "Account");
        }

        model.CourseListItems = await GetCoursesAsync(sponsorId);

        if (courseId.HasValue)
        {
            model.SelectedCourseId = courseId.Value;
            model.CourseInfo = await GetCourseInfoAsync(courseId.Value);
            model.UnregisteredPharmacists = (await GetCompletedPharmacistsAsync(courseId.Value)).ToList();
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
            model.UnregisteredPharmacists = (await GetCompletedPharmacistsAsync(model.SelectedCourseId)).ToList();
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
            .Where(shc => shc.SponsorId == sponsorId &&
                              (shc.Category.HubCourse.CourseType == 0 || shc.CourseType == 1))
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
                .Where(c => c.Id == courseId )
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


    private async Task<IEnumerable<UnregisteredPharmacistViewModel>> GetCompletedPharmacistsAsync(Guid courseId)
    {
        try
        {
           
            var courseInfo = await _mediHub4RumContext.SponsorHubCourse
                .Where(e => e.CategoryId == courseId && e.CourseType == 0)
                .Select(e => new
                {
                    e.TargetStartDate,
                    e.TargetEndDate,
                })
                .FirstOrDefaultAsync();

            if (courseInfo == null)
            {
                // Nếu không tìm thấy khóa học, trả về danh sách rỗng
                return Enumerable.Empty<UnregisteredPharmacistViewModel>();
            }

            // Lấy danh sách người dùng đã hoàn thành khóa học và đã vượt qua
            var userCompletions = await _mediHub4RumContext.SponsorHubCourseFinish
                .Where(e => e.CategoryId == courseId && e.IsPassed == true &&
                            e.Category.HubCourse.CourseType == 0 && 
                            e.FinishDate >= courseInfo.TargetStartDate && e.FinishDate <= courseInfo.TargetEndDate)
                .Select(e => new { e.UserId, e.FinishDate })
                .Distinct()
                .ToListAsync();

            var userIds = userCompletions.Select(uc => uc.UserId).ToList();

            // Lấy thông tin người dùng
            var users = await _mediHub4RumContext.MembershipUser
                .Where(mu => userIds.Contains(mu.Id) && mu.IsTest == false)
                .ToListAsync();

            var appSetups = await _mediHubSCAppContext.AppSetup
                .Where(app => users.Select(u => u.KeyCodeActive).Contains(app.KeyCodeActive))
                .ToListAsync();

            // Ghép nối thông tin người dùng và dữ liệu hoàn thành khóa học
            var completedPharmacists = users.Select(user =>
            {
                var appSetup = appSetups.FirstOrDefault(app => app.KeyCodeActive == user.KeyCodeActive);
                var completion = userCompletions.FirstOrDefault(uc => uc.UserId == user.Id);
                return new UnregisteredPharmacistViewModel
                {
                    TenDuocSi = appSetup?.SCName ?? user.UserName,
                    SoDienThoai = appSetup?.PhoneNumber ?? "",
                    DonViCongTac = appSetup?.DrugName ?? "",
                    DiaChi = appSetup?.Address ?? "",
                    DaXacThuc = false,
                    KeyCodeActive = appSetup?.KeyCodeActive ?? "",
                    NgayHoanThanh = completion.FinishDate,
                };
            }).ToList();

            // Lấy thông tin xác thực của người dùng
            var userInfos = await _mediHub4RumContext.MembershipUserInfo
                .Where(mui => userIds.Contains(mui.UserId))
                .Select(mui => new { mui.UserId, mui.TrangThai })
                .ToListAsync();

            foreach (var pharmacist in completedPharmacists)
            {
                var user = users.FirstOrDefault(u => u.KeyCodeActive == pharmacist.KeyCodeActive);
                if (user != null)
                {
                    var userInfo = userInfos.FirstOrDefault(ui => ui.UserId == user.Id);
                    pharmacist.DaXacThuc = userInfo?.TrangThai == 2;
                }
            }

            completedPharmacists = completedPharmacists
                .OrderByDescending(p => p.NgayHoanThanh)
                .ToList();

            return completedPharmacists;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred in GetCompletedPharmacistsAsync for courseId: {CourseId}", courseId);
            throw;
        }
    }


    [HttpGet("api/unregistered-pharmacists/{courseId}")]
    public async Task<IActionResult> GetUnregisteredPharmacists(Guid courseId)
    {
        try
        {
            var result = await GetCompletedPharmacistsAsync(courseId);
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
            if (courseInfo != null && courseInfo.TargetStartDate.HasValue && courseInfo.TargetEndDate.HasValue)
            {
                result.TargetStartDate = courseInfo.TargetStartDate;
                result.TargetEndDate = courseInfo.TargetEndDate;
                result.TargetFinish = courseInfo.TargetFinish;
                result.TargetJoin = courseInfo.TargetJoin;
                result.CurrentDate = DateTime.Now.Date;
            }
            else
            {
                // Nếu không có dữ liệu trong SponsorHubCourse, lấy từ Category
                var categoryDate = await _mediHub4RumContext.Category
                    .Where(e => e.Id == courseId)
                    .Select(e => e.DateCreated)
                    .FirstOrDefaultAsync();

                if (categoryDate != null)
                {
                    result.TargetStartDate = categoryDate;
                    result.TargetEndDate = DateTime.Now.Date;
                }
            }

            // Tính toán 5 mốc thời gian
            var timePoints = CalculateTimePoints(result.TargetStartDate.Value, result.TargetEndDate.Value);

            result.TotalJoins = new Dictionary<DateTime, int>();
            result.TotalFinishs = new Dictionary<DateTime, int>();
            result.TotalPassed = new Dictionary<DateTime, int>();
            result.TotalEnters = new Dictionary<DateTime, int>();
            result.TotalWatchedAllVideos = new Dictionary<DateTime, int>();

            var lessonIds = await _mediHub4RumContext.Topic
                .Where(t => t.Category_Id == courseId)
                .Select(t => t.Id)
                .ToListAsync();

            foreach (var point in timePoints)
            {
                // Tính tổng số người hoàn thành đến thời điểm này (không phân biệt đạt/không đạt)
                result.TotalFinishs[point] = await _mediHub4RumContext.SponsorHubCourseFinish
                    .Where(e => e.CategoryId == courseId && e.FinishDate < point)
                    .Select(e => e.UserId)
                    .Distinct()
                    .CountAsync();

                // Tính tổng số người vượt qua bài kiểm tra đến thời điểm này
                result.TotalPassed[point] = await _mediHub4RumContext.SponsorHubCourseFinish
                    .Where(e => e.CategoryId == courseId && e.FinishDate < point && e.IsPassed == true)
                    .Select(e => e.UserId)
                    .Distinct()
                    .CountAsync();

                // Tính tổng số người tham gia khóa học
                result.TotalEnters[point] = await _logActionDbContext.LogLesson
                    .Where(ll => lessonIds.Contains(ll.TopicId) && ll.DateAccess < point)
                    .Select(l => l.UserId)
                    .Distinct()
                    .CountAsync();

                // Tính tổng số người xem hết video
                var usersWatchedAllVideos = await _logActionDbContext.LogLesson
                    .Where(ll => lessonIds.Contains(ll.TopicId) && ll.DateAccess < point)
                    .GroupBy(ll => ll.UserId)
                    .Select(g => new
                    {
                        UserId = g.Key,
                        DistinctLessonCount = g.Select(ll => ll.TopicId).Distinct().Count()
                    })
                    .Where(u => u.DistinctLessonCount == lessonIds.Count)
                    .CountAsync();

                result.TotalWatchedAllVideos[point] = usersWatchedAllVideos;
            }

             if (result.CurrentDate.HasValue)
            {
                var currentDate = result.CurrentDate.Value;

                // Tính tổng số người hoàn thành đến thời điểm hiện tại
                result.TotalFinishs[currentDate] = await _mediHub4RumContext.SponsorHubCourseFinish
                    .Where(e => e.CategoryId == courseId && e.FinishDate < currentDate)
                    .Select(e => e.UserId)
                    .Distinct()
                    .CountAsync();

                // Tính tổng số người vượt qua bài kiểm tra đến thời điểm hiện tại
                result.TotalPassed[currentDate] = await _mediHub4RumContext.SponsorHubCourseFinish
                    .Where(e => e.CategoryId == courseId && e.FinishDate < currentDate && e.IsPassed == true)
                    .Select(e => e.UserId)
                    .Distinct()
                    .CountAsync();

                // Tính tổng số người tham gia khóa học đến thời điểm hiện tại
                result.TotalEnters[currentDate] = await _logActionDbContext.LogLesson
                    .Where(ll => lessonIds.Contains(ll.TopicId) && ll.DateAccess < currentDate)
                    .Select(l => l.UserId)
                    .Distinct()
                    .CountAsync();

                // Tính tổng số người xem hết video đến thời điểm hiện tại
                var usersWatchedAllVideos = await _logActionDbContext.LogLesson
                    .Where(ll => lessonIds.Contains(ll.TopicId) && ll.DateAccess < currentDate)
                    .GroupBy(ll => ll.UserId)
                    .Select(g => new
                    {
                        UserId = g.Key,
                        DistinctLessonCount = g.Select(ll => ll.TopicId).Distinct().Count()
                    })
                    .Where(u => u.DistinctLessonCount == lessonIds.Count)
                    .CountAsync();

                result.TotalWatchedAllVideos[currentDate] = usersWatchedAllVideos;
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
        model.UnregisteredPharmacists = (await GetCompletedPharmacistsAsync(courseId)).ToList();
        model.AchieveTargets = await GetAchieveTargetsAsync(courseId);

        return PartialView("_CourseStatistics", model);
    }

}
