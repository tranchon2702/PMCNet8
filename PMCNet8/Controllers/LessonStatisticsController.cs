using System.Globalization;
using Data.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using PMCNet8.Models;

namespace PMCNet8.Controllers
{
    public class LessonStatisticsController : Controller
    {
        private readonly LogActionDbContext _logActionDbContext;
        private readonly Medihub4rumDbContext _mediHub4RumContext;
        private readonly MedihubSCAppDbContext _mediHubSCAppContext;
        private readonly ILogger<LessonStatisticsController> _logger;
        private readonly double passingRatio;

        public LessonStatisticsController(LogActionDbContext logActionDbContext, Medihub4rumDbContext mediHub4RumContext, MedihubSCAppDbContext mediHubSCAppContext, ILogger<LessonStatisticsController> logger)
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
                passingRatio = 0.5;
            }
        }

        public async Task<IActionResult> Index()
        {
            if (!Guid.TryParse(HttpContext.Session.GetString("SponsorId"), out Guid sponsorId))
            {
                return BadRequest("Invalid SponsorId");
            }

            var lessons = await GetLessonsAsync(sponsorId);
            return View(lessons);
        }

        [HttpGet]
        public async Task<IActionResult> GetLessonStatistics(Guid lessonId, DateTime startDate, DateTime endDate)
        {
            try
            {
                if (!Guid.TryParse(HttpContext.Session.GetString("SponsorId"), out Guid sponsorId))
                {
                    return BadRequest("Invalid SponsorId");
                }

                var lesson = await _mediHub4RumContext.SponsorHubCourse
                    .Where(shc => shc.SponsorId == sponsorId)
                    .SelectMany(shc => shc.Category.Topics)
                    .Where(t => t.Id == lessonId)
                    .Select(t => new { t.Name, CourseName = t.Category.Name })
                    .FirstOrDefaultAsync();

                if (lesson == null)
                {
                    return NotFound("Lesson not found or not accessible");
                }

                var chartData = await GetChartDataAsync(lessonId, startDate, endDate);
                var tableData = await GetTableDataAsync(lessonId, startDate, endDate);

                var model = new LessonStatisticsViewModel
                {
                    LessonName = lesson.Name,
                    CourseName = lesson.CourseName,
                    ChartData = chartData,
                    TableData = tableData,
                    StartDate = startDate,
                    EndDate = endDate
                };

                return PartialView("_LessonStatistics", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching lesson statistics");
                return StatusCode(500, "An error occurred while processing your request");
            }
        }

        private async Task<List<LessonListItem>> GetLessonsAsync(Guid sponsorId)
        {
            return await _mediHub4RumContext.SponsorHubCourse
                .Where(shc => shc.SponsorId == sponsorId)
                .SelectMany(shc => shc.Category.Topics)
                .Select(t => new LessonListItem
                {
                    Id = t.Id,
                    Name = t.Name,
                    CourseName = t.Category.Name
                })
                .ToListAsync();
        }

        private async Task<ChartDataViewModel> GetChartDataAsync(Guid lessonId, DateTime startDate, DateTime endDate)
        {
            var lessonLogs = await _logActionDbContext.LogLesson
                .Where(ll => ll.TopicId == lessonId &&
                             ll.DateAccess.Date >= startDate.Date &&
                             ll.DateAccess.Date <= endDate.Date)
                .ToListAsync();

            var listQuestions = await _mediHub4RumContext.MediHubSCQuiz
                .Where(t => t.TopicId == lessonId)
                .Select(t => t.QuizQuestions)
                .Distinct()
                .FirstOrDefaultAsync();
            var questions = JsonConvert.DeserializeObject<List<LessonQuestion>>(listQuestions ?? string.Empty);

            return new ChartDataViewModel
            {
                Joins = lessonLogs.Count(l => l.Status == "Access"),
                CompletedLesson = lessonLogs.Count(l => l.Status == "Completed"),
                CompleteTest = lessonLogs.Count(l => l.Status == "PostTest" && IsPassingScore(l.Result)),
                FailedTest = lessonLogs.Count(l => l.Status == "PostTest" && !IsPassingScore(l.Result)),
                TotalQuestions = questions?.Count ?? 0
            };
        }

        private async Task<List<LessonUserActivityViewModel>> GetTableDataAsync(Guid lessonId, DateTime startDate, DateTime endDate)
        {
            var userLessons = await _logActionDbContext.LogLesson
                .Where(ll => ll.TopicId == lessonId &&
                             ll.DateAccess.Date >= startDate.Date &&
                             ll.DateAccess.Date <= endDate.Date)
                .Select(ll => new { ll.UserId, ll.Status, ll.Result, ll.DateAccess })
                .ToListAsync();

            var userIds = userLessons.Select(ul => ul.UserId).Distinct().ToList();

            var users = await _mediHub4RumContext.MembershipUser
                .Where(mu => userIds.Contains(mu.Id))
                .ToListAsync();

            var appSetups = await _mediHubSCAppContext.AppSetup
                .Where(app => users.Select(u => u.KeyCodeActive).Contains(app.KeyCodeActive))
                .ToListAsync();

            return users.Select(user =>
            {
                var appSetup = appSetups.FirstOrDefault(app => app.KeyCodeActive == user.KeyCodeActive);
                var userLesson = userLessons.FirstOrDefault(ul => ul.UserId == user.Id);
                return new LessonUserActivityViewModel
                {
                    TenDuocSi = appSetup?.SCName ?? user.UserName,
                    SoDienThoai = appSetup?.PhoneNumber ?? "",
                    Email = user.Email,
                    DiaChi = appSetup?.Address ?? "",
                    DonViCongTac = appSetup?.DrugName ?? "",
                    HanhDong = GetActivityStatus(userLesson?.Status, IsPassingScore(userLesson?.Result)),
                    Ngay = userLesson?.DateAccess.ToString("dd/MM/yyyy HH:mm") ?? ""
                };
            }).ToList();
        }

        private bool IsPassingScore(string result)
        {
            if (string.IsNullOrEmpty(result)) return false;

            string[] parts = result.Split('/');
            if (parts.Length != 2) return false;

            if (!double.TryParse(parts[0], out double numerator) || !double.TryParse(parts[1], out double denominator))
                return false;

            if (denominator == 0) return false;

            return numerator / denominator >= passingRatio;
        }

        private string GetActivityStatus(string status, bool isPassing)
        {
            return status switch
            {
                "Access" => "Vào bài học",
                "Completed" => "Đã học xong bài học",
                "PostTest" when isPassing => "Đã làm bài tập-Đạt",
                "PostTest" when !isPassing => "Đã làm bài tập-Chưa đạt",
                _ => "Không xác định"
            };
        }
    }
}