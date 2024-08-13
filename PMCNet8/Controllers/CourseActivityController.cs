using Data.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PMCNet8.Models;
using System.Globalization;
using System.Runtime.InteropServices;


    public class CourseActivityController : Controller
    {
        private readonly LogActionDbContext _logActionDbContext;
        private readonly Medihub4rumDbContext _mediHub4RumContext;
        private readonly MedihubSCAppDbContext _mediHubSCAppContext;
        private readonly ILogger<CourseActivityController> _logger;
        private readonly double passingRatio;
    public CourseActivityController(LogActionDbContext logActionDbContext, Medihub4rumDbContext mediHub4RumContext, MedihubSCAppDbContext mediHubSCAppContext, ILogger<CourseActivityController> logger)
        {
            _logActionDbContext = logActionDbContext;
            _mediHub4RumContext = mediHub4RumContext;
            _mediHubSCAppContext = mediHubSCAppContext;
            _logger = logger;
        var percentToPassString = _mediHubSCAppContext.Config
        .Where(e => e.Id == "PercentToPassQuiz")
        .Select(e => e.Value)
        .FirstOrDefault();

        if (!double.TryParse(percentToPassString, NumberStyles.Any, CultureInfo.InvariantCulture, out passingRatio))
        {
            // Nếu không parse được, sử dụng giá trị mặc định
            passingRatio = 0.5;
        }

    }

        public async Task<IActionResult> Index()
        {
            if (!Guid.TryParse(HttpContext.Session.GetString("SponsorId"), out Guid sponsorId))
            {
                return BadRequest("Invalid SponsorId");
            }

            var courses = await GetCoursesAsync(sponsorId);
            return View(courses);
        }

        [HttpGet]
        public async Task<IActionResult> GetCourseActivity(Guid courseId, DateTime startDate, DateTime endDate)
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

                var chartData = await GetChartDataAsync(topics, startDate, endDate);
                var tableData = await GetTableDataAsync(courseId, startDate, endDate);

                var model = new CourseActivityViewModel
                {
                    CourseName = courseName,
                    ChartData = chartData,
                    TableData = tableData,
                    StartDate = startDate,
                    EndDate = endDate
                };

                _logger.LogInformation($"Returning PartialView with ChartData: {System.Text.Json.JsonSerializer.Serialize(chartData)}");
                return PartialView("_CourseActivity", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching course activity data");
                return StatusCode(500, "An error occurred while processing your request");
            }
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

        private async Task<List<ChartDataViewModel>> GetChartDataAsync(List<TopicInfo> topics, DateTime startDate, DateTime endDate)
        {
            var chartData = new List<ChartDataViewModel>();
            foreach (var topic in topics)
            {
                var topicLogs = await _logActionDbContext.LogLesson
                    .Where(ll => ll.TopicId == topic.Id &&
                                 ll.DateAccess.Date >= startDate.Date 
                                 && ll.DateAccess.Date <= endDate.Date)
                    .ToListAsync();


                chartData.Add(new ChartDataViewModel
                {
                    Lesson = topic.Name,
                    Joins = topicLogs.Count(l => l.Status == "Access"),
                    CompleteTest = topicLogs.Count(l => l.Status == "PostTest" && IsPassingScore(l.Result)),
                    FailedTest = topicLogs.Count(l => l.Status == "PostTest" && !IsPassingScore(l.Result))
                });
            }
            return chartData;
        }
    private bool IsPassingScore(string result)
    {
        if (string.IsNullOrEmpty(result))
        {
            return false;
        }

        string[] parts = result.Split('/');
        if (parts.Length != 2)
        {
            return false;
        }

        if (!double.TryParse(parts[0], out double numerator) || !double.TryParse(parts[1], out double denominator))
        {
            return false;
        }

        if (denominator == 0)
        {
            return false;
        }

        return numerator / denominator >= passingRatio;
    }
    private async Task<List<CourseActivityViewModel>> GetTableDataAsync(Guid courseId, DateTime startDate, DateTime endDate)
        {
            var topicIds = await _mediHub4RumContext.Topic
                .Where(t => t.Category_Id == courseId)
                .Select(t => t.Id)
                .ToListAsync();

            var topicCount = topicIds.Count;

            var userLessons = await _logActionDbContext.LogLesson
                .Where(ll => topicIds.Contains(ll.TopicId) &&
                             ll.DateAccess >= startDate &&
                             ll.DateAccess <= endDate &&
                             ll.Status == "PostTest")
                .Select(ll => new { ll.UserId, ll.TopicId, ll.Result })
                .ToListAsync();

            var userCompletions = userLessons
                .GroupBy(ll => ll.UserId)
                .Select(g => new
                {
                    UserId = g.Key,
                    CompletedTopics = g.Where(l => !string.IsNullOrEmpty(l.Result) &&
                                                   double.TryParse(l.Result, out var score) &&
                                                   score >= 0.5)
                                       .Select(l => l.TopicId)
                                       .Distinct()
                                       .Count()
                })
                .Where(uc => uc.CompletedTopics == topicCount)
                .ToList();

            var completedUserIds = userCompletions.Select(uc => uc.UserId).ToList();

            var users = await _mediHub4RumContext.MembershipUser
                .Where(mu => completedUserIds.Contains(mu.Id))
                .ToListAsync();

            var appSetups = await _mediHubSCAppContext.AppSetup
                .Where(app => users.Select(u => u.KeyCodeActive).Contains(app.KeyCodeActive))
                .ToListAsync();

            return users.Select(user =>
            {
                var appSetup = appSetups.FirstOrDefault(app => app.KeyCodeActive == user.KeyCodeActive);
                return new CourseActivityViewModel
                {
                    TenDuocSi = appSetup?.SCName ?? user.UserName,
                    SoDienThoai = appSetup?.PhoneNumber ?? "",
                    Email = user.Email,
                    DiaChi = appSetup?.Address ?? "",
                    DonViCongTac = appSetup?.DrugName ?? "",
                    KetQua = $"{topicCount}/{topicCount}"
                };
            }).ToList();
        }
    }
