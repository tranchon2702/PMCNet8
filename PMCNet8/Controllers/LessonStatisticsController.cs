using System.Globalization;
using Data.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using PMCNet8.Models;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

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
                //return BadRequest("Invalid SponsorId");
                return RedirectToAction("Login", "Account");
            }

            var lessons = await _mediHub4RumContext.Topic
                .Where(e => e.Category.HubCourse != null && e.Category.HubCourse.SponsorId == sponsorId)
                .OrderBy(e => e.Order).ThenBy(t => t.Name)
                .Select(t => new LessonListItem
                {
                    Id = t.Id,
                    Name = t.Name,
                    CourseId = t.Category_Id,
                    CourseName = t.Category.Name
                })
                .ToListAsync();

            return View(lessons);
        }

        [HttpGet]
        public async Task<IActionResult> GetLessonStatistics(Guid lessonId, string startDate = null, string endDate = null)
        {
            try
            {
                DateTime? parsedStartDate = null;
                DateTime? parsedEndDate = null;

                if (!string.IsNullOrEmpty(startDate) && !string.IsNullOrEmpty(endDate))
                {
                    parsedStartDate = DateTime.ParseExact(startDate, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                    parsedEndDate = DateTime.ParseExact(endDate, "dd/MM/yyyy", CultureInfo.InvariantCulture).AddDays(1).AddSeconds(-1);
                }

                if (!Guid.TryParse(HttpContext.Session.GetString("SponsorId"), out Guid sponsorId))
                {
                    return BadRequest("Invalid SponsorId");
                }

                var lesson = await _mediHub4RumContext.Topic
                .Where(e => e.Category.HubCourse != null && e.Category.HubCourse.SponsorId == sponsorId && e.Id == lessonId)
                .Select(t => new LessonListItem
                {
                    Id = t.Id,
                    Name = t.Name,
                    CourseId = t.Category_Id,
                    CourseName = t.Category.Name
                })
                .FirstOrDefaultAsync();

                if (lesson == null)
                {
                    return NotFound("Lesson not found or not accessible");
                }

                var chartData = await GetChartDataAsync(lessonId, parsedStartDate, parsedEndDate);
                var tableData = await GetTableDataAsync(lessonId, parsedStartDate, parsedEndDate);

                var model = new LessonStatisticsViewModel
                {
                    LessonName = lesson.Name,
                    CourseName = lesson.CourseName,
                    ChartData = chartData,
                    TableData = tableData,
                    StartDate = parsedStartDate,
                    EndDate = parsedEndDate
                };

                return PartialView("_LessonStatistics", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching lesson statistics");
                return StatusCode(500, "An error occurred while processing your request");
            }
        }

        private async Task<ChartLessonViewModel> GetChartDataAsync(Guid lessonId, DateTime? parsedStartDate, DateTime? parsedEndDate)
        {
            var query = _logActionDbContext.LogLesson
                .Where(ll => ll.TopicId == lessonId);

            if (parsedStartDate.HasValue)
                query = query.Where(ll => ll.DateAccess.Date >= parsedStartDate.Value.Date);
            if (parsedEndDate.HasValue)
                query = query.Where(ll => ll.DateAccess.Date <= parsedEndDate.Value.Date);

            var logLessons = await query.ToListAsync();

            var listQuestions = await _mediHub4RumContext.MediHubSCQuiz
                .Where(t => t.TopicId == lessonId)
                .Select(t => t.QuizQuestions)
                .FirstOrDefaultAsync();

            var questions = JsonConvert.DeserializeObject<List<LessonQuestion>>(listQuestions ?? string.Empty);

            var data = new ChartLessonViewModel
            {
                Joins = logLessons.Select(ll => ll.UserId).Distinct().Count(),
                CompletedLesson = logLessons.Where(l => l.Status == "Finish").Select(ll => ll.UserId).Distinct().Count(),
                DoTest = logLessons.Where(l => l.Status == "PostTest").Select(ll => ll.UserId).Distinct().Count(),
                CompleteTest = logLessons.Where(l => l.Status == "PostTest" && IsPassingScore(l.Result)).Select(ll => ll.UserId).Distinct().Count(),
                //FailedTest = logLessons.Count(l => l.Status == "PostTest" && !IsPassingScore(l.Result)),
                TotalQuestions = questions?.Count ?? 0
            };

            if (data.Joins < data.CompletedLesson) data.CompletedLesson = data.Joins;
            if (data.CompletedLesson < data.DoTest) data.DoTest = data.CompletedLesson;
            if (data.DoTest < data.CompleteTest) data.CompleteTest = data.DoTest;
            data.FailedTest =  (data.DoTest - data.CompleteTest); //ko làm + làm sai
            data.NotTest = (data.CompletedLesson - data.DoTest);

            return data;
        }

        private async Task<List<LessonUserActivityViewModel>> GetTableDataAsync(Guid lessonId, DateTime? parsedStartDate, DateTime? parsedEndDate)
        {
            var userQuery = _logActionDbContext.LogLesson
                .Where(ll => ll.TopicId == lessonId);
            if (parsedStartDate.HasValue)
                userQuery = userQuery.Where(ll => ll.DateAccess.Date >= parsedStartDate.Value.Date);
            if (parsedEndDate.HasValue)
                userQuery = userQuery.Where(ll => ll.DateAccess.Date <= parsedEndDate.Value.Date);
            var userLessons = await userQuery.Select(ll => new { ll.UserId, ll.Status, ll.Result, ll.DateAccess }).ToListAsync();
            var userIds = userLessons.Select(ul => ul.UserId).Distinct().ToList();
            var users = await _mediHub4RumContext.MembershipUser
                .Where(mu => userIds.Contains(mu.Id) && mu.IsTest == false)
                .ToListAsync();
            var appSetups = await _mediHubSCAppContext.AppSetup
                .Where(app => users.Select(u => u.KeyCodeActive).Contains(app.KeyCodeActive))
                .ToListAsync();

            var result = users.Select(user =>
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
                    HanhDong = GetActivityStatus(userLesson.Status, IsPassingScore(userLesson.Result)),
                    Ngay = userLesson.DateAccess,
                };
            }).ToList();

            return result.OrderByDescending(r => r.Ngay).ToList();
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
                "Finish" => "Đã học xong bài học",
                "PostTest" when isPassing => "Đã làm bài tập-Đạt",
                "PostTest" when !isPassing => "Đã làm bài tập-Chưa đạt",
                _ => "Không xác định"
            };
        }
    }
}