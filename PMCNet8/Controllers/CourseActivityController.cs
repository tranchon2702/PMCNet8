using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Data.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PMCNet8.Models;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace PMCNet8.Controllers
{
    public class CourseActivityController : BaseController
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
            var sponsorId = GetSponsorId();  
            if (sponsorId == Guid.Empty)
            {
                return RedirectToAction("Login", "Account");
            }

            var courses = await _mediHub4RumContext.SponsorHubCourse
               .Where(shc => shc.SponsorId == sponsorId &&
                              (shc.Category.HubCourse.CourseType == 0 || shc.CourseType == 1))
               .Select(shc => new CourseListItems
               {
                   Id = shc.CategoryId,
                   Name = shc.Category.Name
               })
               .ToListAsync();

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
                    .OrderBy(t => t.Order).ThenBy(e => e.Name)
                    .Select(t => new TopicInfo { Id = t.Id, Name = t.Name })
                    .ToListAsync();

                // chart
                var chartData = new List<ChartDataViewModel>();
                foreach (var topic in topics)
                {
                    var query = _logActionDbContext.LogLesson.Where(ll => ll.TopicId == topic.Id);
                    if (parsedStartDate.HasValue && parsedEndDate.HasValue)
                    {
                        query = query.Where(ll => ll.DateAccess.Date >= parsedStartDate.Value.Date && ll.DateAccess.Date <= parsedEndDate.Value.Date);
                    }
                    var topicLogs = await query.ToListAsync();

                    var lesson = new ChartDataViewModel
                    {
                        Lesson = topic.Name,
                        Joins = topicLogs.Select(l => l.UserId).Distinct().Count(userId => topicLogs.Any(l => l.UserId == userId)),
                        CompleteTest = topicLogs.Where(l => l.Status == "PostTest").Select(ll => ll.UserId).Distinct().Count(),
                    };
                    if (lesson.Joins < lesson.CompleteTest) lesson.CompleteTest = lesson.Joins;
                    lesson.FailedTest = lesson.Joins - lesson.CompleteTest;

                    chartData.Add(lesson);
                }


                // table
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

        private async Task<List<CourseActivityViewModel>> GetTableDataAsync(Guid courseId, DateTime? startDate, DateTime? endDate)
        {
            var query = _mediHub4RumContext.SponsorHubCourseFinish
                .Where(e => e.CategoryId == courseId);
            if (startDate.HasValue && endDate.HasValue)
            {
                query = query.Where(e => e.FinishDate.Date >= startDate.Value.Date && e.FinishDate.Date <= endDate.Value.Date);
            }
            var userCompletions = await query.Select(e => new { e.UserId, e.IsPassed, e.FinishDate }).ToListAsync();
            var userIds = userCompletions.Select(e => e.UserId).ToList();
            var users = await _mediHub4RumContext.MembershipUser
                .Where(mu => userIds.Contains(mu.Id) && mu.IsTest == false)
                .ToListAsync();
            var appSetups = await _mediHubSCAppContext.AppSetup
                .Where(app => users.Select(u => u.KeyCodeActive).Contains(app.KeyCodeActive))
                .ToListAsync();
            var topicCount = await _mediHub4RumContext.Topic
                .Where(t => t.Category_Id == courseId)
                .CountAsync();
            var result = users.Select(user =>
            {
                var appSetup = appSetups.FirstOrDefault(app => app.KeyCodeActive == user.KeyCodeActive);
                var userCompletion = userCompletions.FirstOrDefault(e => e.UserId == user.Id);
                return new CourseActivityViewModel
                {
                    TenDuocSi = appSetup?.SCName ?? user.UserName,
                    SoDienThoai = appSetup?.PhoneNumber ?? "",
                    Email = user.Email,
                    DiaChi = appSetup?.Address ?? "",
                    DonViCongTac = appSetup?.DrugName ?? "",
                    KetQua = userCompletion?.IsPassed == true ? "Đạt" : "Không đạt",
                    NgayHoanThanh = userCompletion.FinishDate  
                };
            }).ToList();

            return result.OrderByDescending(r => r.NgayHoanThanh).ToList();
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