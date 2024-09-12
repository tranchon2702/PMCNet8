using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Data.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PMCNet8.Models;

namespace PMCNet8.Controllers
{
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
                passingRatio = 0.5; // Default value if parsing fails
            }
        }

        public async Task<IActionResult> Index()
        {
            if (!Guid.TryParse(HttpContext.Session.GetString("SponsorId"), out Guid sponsorId))
            {
                return BadRequest("Invalid SponsorId");
            }

            var courses = await GetCoursesAsync(sponsorId);

            // Get the first course ID to load initial data
            var firstCourseId = courses.FirstOrDefault()?.Id;

            ViewBag.FirstCourseId = firstCourseId;

            return View(courses);
        }

        [HttpGet]
        public async Task<IActionResult> GetCourseActivity(Guid courseId, string startDate = null, string endDate = null)
        {
            try
            {
                DateTime? parsedStartDate = null;
                DateTime? parsedEndDate = null;

                if (!string.IsNullOrEmpty(startDate) && !string.IsNullOrEmpty(endDate))
                {
                    parsedStartDate = DateTime.ParseExact(startDate, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                    parsedEndDate = DateTime.ParseExact(endDate, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                }

                var courseName = await _mediHub4RumContext.Category
                    .Where(c => c.Id == courseId)
                    .Select(c => c.Name)
                    .FirstOrDefaultAsync();

                var topics = await _mediHub4RumContext.Topic
                    .Where(t => t.Category_Id == courseId)
                    .Select(t => new TopicInfo { Id = t.Id, Name = t.Name , Order = t.Order })
                    .ToListAsync();

                var chartData = await GetChartDataAsync(topics, parsedStartDate, parsedEndDate);
                var tableData = await GetTableDataAsync(courseId, parsedStartDate, parsedEndDate);

                var model = new CourseActivityViewModel
                {
                    CourseName = courseName,
                    ChartData = chartData,
                    TableData = tableData,
                    StartDate = parsedStartDate,
                    EndDate = parsedEndDate
                };

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

       
        private async Task<List<ChartDataViewModel>> GetChartDataAsync(List<TopicInfo> topics, DateTime? startDate, DateTime? endDate)
        {
            
            topics = topics.OrderBy(t => t.Order).ToList();

            var chartData = new List<ChartDataViewModel>();
            foreach (var topic in topics)
            {
                var query = _logActionDbContext.LogLesson.Where(ll => ll.TopicId == topic.Id);
                if (startDate.HasValue && endDate.HasValue)
                {
                    query = query.Where(ll => ll.DateAccess.Date >= startDate.Value.Date && ll.DateAccess.Date <= endDate.Value.Date);
                }

                var topicLogs = await query.ToListAsync();

                chartData.Add(new ChartDataViewModel
                {
                    Lesson = topic.Name,
                    Joins = topicLogs.Select(l => l.UserId).Distinct().Count(userId => topicLogs.Any(l => l.UserId == userId && l.Status == "Access")),
                    CompleteTest = topicLogs.Count(l => l.Status == "PostTest" && IsPassingScore(l.Result)),
                    FailedTest = topicLogs.Count(l => l.Status == "PostTest" && !IsPassingScore(l.Result))
                });
            }
            return chartData;
        }
        private async Task<List<CourseActivityViewModel>> GetTableDataAsync(Guid courseId, DateTime? startDate, DateTime? endDate)
        {
            var query = _mediHub4RumContext.SponsorHubCourseFinish
                .Where(e => e.CategoryId == courseId);

            if (startDate.HasValue && endDate.HasValue)
            {
                query = query.Where(e => e.FinishDate.Date >= startDate.Value.Date && e.FinishDate.Date <= endDate.Value.Date);
            }

            var userCompletions = await query.Select(e => e.UserId).ToListAsync();

            var users = await _mediHub4RumContext.MembershipUser
                .Where(mu => userCompletions.Contains(mu.Id)&& mu.IsTest == false)
                .ToListAsync();

            var appSetups = await _mediHubSCAppContext.AppSetup
                .Where(app => users.Select(u => u.KeyCodeActive).Contains(app.KeyCodeActive))
                .ToListAsync();

            var topicCount = await _mediHub4RumContext.Topic
                .Where(t => t.Category_Id == courseId)
                .CountAsync();

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
    }
}