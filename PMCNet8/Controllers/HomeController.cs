﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Data.Data;
using Data.Medihub4rumEntities;
using System;
using System.Linq;
using System.Threading.Tasks;
using PMCNet8.Models;
using System.Runtime.ConstrainedExecution;


namespace PMCNet8.Controllers
{
    
    public class HomeController : Controller
    {
        private readonly LogActionDbContext _logActionContext;
        private readonly Medihub4rumDbContext _mediHub4RumContext;
        private readonly MedihubSCAppDbContext _mediHubSCAppContext;
        private readonly ILogger<HomeController> _logger;
        public HomeController(LogActionDbContext logActionContext, Medihub4rumDbContext mediHub4RumContext , MedihubSCAppDbContext mediHubSCAppContext, ILogger<HomeController> logger)
        {
            _logActionContext = logActionContext;
            _mediHub4RumContext = mediHub4RumContext;
            _mediHubSCAppContext = mediHubSCAppContext;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            if (!Guid.TryParse(HttpContext.Session.GetString("SponsorId"), out Guid sponsorId))
            {
                return BadRequest("Invalid SponsorId");
            }

            var model = new HomeViewModel
            {
                AvailableCourseTypes = await GetAvailableCourseTypesAsync(sponsorId)
            };
            return View(model);
        }

        private async Task<List<int>> GetAvailableCourseTypesAsync(Guid sponsorId)
        {
            return await _mediHub4RumContext.SponsorHubCourse
                .Where(shc => shc.SponsorId == sponsorId)
                .Select(shc => shc.CourseType)
                .Distinct()
                .ToListAsync();
        }

        [HttpGet("api/participantCount/month")]
        public async Task<IActionResult> GetParticipantCountForMonth()
        {
            if (!Guid.TryParse(HttpContext.Session.GetString("SponsorId"), out Guid sponsorId))
            {
                return BadRequest("Invalid SponsorId");
            }
            int count  = await GetUserStudyForMonthAsync(sponsorId);
            return Ok(count);
        }

        [HttpGet("api/participantCount/year")]
        public async Task<IActionResult> GetParticipantCountForYear()
        {
            if (!Guid.TryParse(HttpContext.Session.GetString("SponsorId"), out Guid sponsorId))
            {
                return BadRequest("Invalid SponsorId");
            }
            int count = await GetUserStudyForYearAsync(sponsorId);
            return Ok(count);
        }

        [HttpGet("api/totalCertificatesCategory/month")]
        public async Task<IActionResult> GetTotalCertificatesCategoryMonth()
        {
            if (!Guid.TryParse(HttpContext.Session.GetString("SponsorId"), out Guid sponsorId))
            {
                return BadRequest("Invalid SponsorId");
            }

            var data = await GetTotalCertificatesCategoryForMonthAsync(sponsorId);
                return Ok(data);
            
        }
        [HttpGet("api/totalCertificatesCategory/year")]
        public async Task<IActionResult> GetTotalCertificatesCategoryYear()
        {
            if (!Guid.TryParse(HttpContext.Session.GetString("SponsorId"), out Guid sponsorId))
            {
                return BadRequest("Invalid SponsorId");
            }

            var data = await GetTotalCertificatesCategoryForYearAsync(sponsorId);
            return Ok(data);

        }
        [HttpGet("api/totalNonCertificatesCategory/month")]
        public async Task<IActionResult> GetTotalNonCertificatesCategoryMonth()
        {
            if (!Guid.TryParse(HttpContext.Session.GetString("SponsorId"), out Guid sponsorId))
            {
                return BadRequest("Invalid SponsorId");
            }

            var data = await GetTotalNonCertificatesCategoryForMonthAsync(sponsorId);
            return Ok(data);

        }
        [HttpGet("api/totalNonCertificatesCategory/year")]
        public async Task<IActionResult> GetTotalNonCertificatesCategoryYear()
        {
            if (!Guid.TryParse(HttpContext.Session.GetString("SponsorId"), out Guid sponsorId))
            {
                return BadRequest("Invalid SponsorId");
            }

            var data = await GetTotalNonCertificatesCategoryForYearAsync(sponsorId);
            return Ok(data);

        }
        [HttpGet("api/totalUpdateCategory/month")]
        public async Task<IActionResult> GetTotalUpdateCategoryMonth()
        {
            if (!Guid.TryParse(HttpContext.Session.GetString("SponsorId"), out Guid sponsorId))
            {
                return BadRequest("Invalid SponsorId");
            }

            var data = await GetTotalUpdateCategoryForMonthAsync(sponsorId);
            return Ok(data);

        }
        [HttpGet("api/totalUpdateCategory/year")]
        public async Task<IActionResult> GetTotalUpdateCategoryYear()
        {
            if (!Guid.TryParse(HttpContext.Session.GetString("SponsorId"), out Guid sponsorId))
            {
                return BadRequest("Invalid SponsorId");
            }

            var data = await GetTotalUpdateCategoryForYearAsync(sponsorId);
            return Ok(data);

        }
        [HttpGet("api/completedCourses/month")]
        public async Task<IActionResult> GetCompletedCoursesForMonth()
        {
            if (!Guid.TryParse(HttpContext.Session.GetString("SponsorId"), out Guid sponsorId))
            {
                return BadRequest("Invalid SponsorId");
            }
            var now = DateTime.Now;
            var firstDayOfMonth = new DateTime(now.Year, now.Month, 1);
            int count = await GetTotalUsersCompletedCoursesAsync(firstDayOfMonth, now, sponsorId);
            return Ok(count);
        }

        [HttpGet("api/completedCourses/year")]
        public async Task<IActionResult> GetCompletedCoursesForYear()
        {
            if (!Guid.TryParse(HttpContext.Session.GetString("SponsorId"), out Guid sponsorId))
            {
                return BadRequest("Invalid SponsorId");
            }
            var now = DateTime.Now;
            var firstDayOfYear = new DateTime(now.Year, 1, 1);
            int count = await GetTotalUsersCompletedCoursesAsync(firstDayOfYear, now, sponsorId);
            return Ok(count);
        }

        [HttpGet("api/totalUserGetCertificates/month")]
        public async Task<IActionResult> GetTotalUsersGetCertificatesForMonth()
        {
            if (!Guid.TryParse(HttpContext.Session.GetString("SponsorId"), out Guid sponsorId))
            {
                return BadRequest("Invalid SponsorId");
            }
            var now = DateTime.Now;
            var firstDayOfMonth = new DateTime(now.Year, now.Month, 1);
            int count = await GetTotalUsersGetCertificatesAsync(firstDayOfMonth, now, sponsorId);
            return Ok(count);
        }

        [HttpGet("api/totalUserGetCertificates/year")]
        public async Task<IActionResult> GetTotalUsersGetCertificatesForYear()
        {
            if (!Guid.TryParse(HttpContext.Session.GetString("SponsorId"), out Guid sponsorId))
            {
                return BadRequest("Invalid SponsorId");
            }
            var now = DateTime.Now;
            var firstDayOfYear = new DateTime(now.Year, 1, 1);
            int count = await GetTotalUsersGetCertificatesAsync(firstDayOfYear, now, sponsorId);
            return Ok(count);
        }
        
        private async Task<UpdateChartViewModel> GetTotalUpdateCategoryForMonthAsync(Guid sponsorId)
        {
            var model = new UpdateChartViewModel();
            var now = DateTime.Now;
            var firstDayOfMonth = new DateTime(now.Year, now.Month, 1);

            int currentYear = now.Year;
            int currentMonth = now.Month;
            try
            {
               

                var categoryIds = await _mediHub4RumContext.SponsorHubCourse
                    .Where(e => e.Category.HubCourse.CourseType == 2
                                && e.Category.HubCourse.SponsorId == sponsorId)
                    .Select(e => e.CategoryId)
                    .ToListAsync();

                var lessonIds = await _mediHub4RumContext.Topic
                     .Where(t =>  categoryIds.Contains(t.Category_Id))
                     .Select(t => t.Id)
                     .ToListAsync();

                model.Watched = await _logActionContext.LogLesson
                    .Where(e => lessonIds.Contains ( e.TopicId) 
                    && firstDayOfMonth <= e.DateAccess && e.DateAccess <= now)
                    .Select(e => e.UserId)
                    .Distinct()
                    .CountAsync();


                model.Finish = await _mediHub4RumContext.SponsorHubCourseReport
                        .Where(cfr => cfr.Category.HubCourse != null &&
                                      cfr.Category.HubCourse.SponsorId == sponsorId &&
                                      cfr.Category.HubCourse.CourseType == 2 &&
                                      currentYear == cfr.Year && currentMonth == cfr.Month)
                        .SumAsync(e => e.FinishCourse);
                return model;
            }
            catch (Exception ex)
            {
                return new UpdateChartViewModel();
            }
        }
        private async Task<UpdateChartViewModel> GetTotalUpdateCategoryForYearAsync(Guid sponsorId)
        {
            var model = new UpdateChartViewModel();
            var now = DateTime.Now;
            var firstDayOfYear = new DateTime(now.Year, 1, 1);

            int currentYear = now.Year;
            int currentMonth = now.Month;
            try
            {
                var categoryIds = await _mediHub4RumContext.SponsorHubCourse
                    .Where(e => e.Category.HubCourse.CourseType == 2
                                && e.Category.HubCourse.SponsorId == sponsorId)
                    .Select(e => e.CategoryId)
                    .ToListAsync();

                var lessonIds = await _mediHub4RumContext.Topic
                     .Where(t => categoryIds.Contains(t.Category_Id))
                     .Select(t => t.Id)
                     .ToListAsync();

                model.Watched = await _logActionContext.LogLesson
                    .Where(e => lessonIds.Contains(e.TopicId)
                    && firstDayOfYear <= e.DateAccess && e.DateAccess <= now)
                    .Select(e => e.UserId)
                    .Distinct()
                    .CountAsync();
                model.Finish = await _mediHub4RumContext.SponsorHubCourseReport
                        .Where(cfr => cfr.Category.HubCourse != null &&
                                      cfr.Category.HubCourse.SponsorId == sponsorId &&
                                      cfr.Category.HubCourse.CourseType == 2 &&
                                      currentYear == cfr.Year)
                        .SumAsync(e => e.FinishCourse);
                return model;
            }
            catch (Exception ex)
            {
                return new UpdateChartViewModel();
            }
        }
        private async Task<CertificateModel> GetTotalCertificatesCategoryForMonthAsync(Guid sponsorId)
        {
            var model = new CertificateModel();
            var now = DateTime.Now;
            var firstDayOfMonth = new DateTime(now.Year, now.Month, 1);
            int currentYear = now.Year;
            int currentMonth = now.Month;
            try {
                var categoryIds = await _mediHub4RumContext.SponsorHubCourse
                   .Where(shc => shc.SponsorId == sponsorId && shc.Category.HubCourse.CourseType == 0)
                   .Select(shc => shc.CategoryId)
                   .ToListAsync();
                var lessonIds = await _mediHub4RumContext.Topic
                .Where(t => categoryIds.Contains(t.Category_Id))
                .Select(t => t.Id)
                .ToListAsync();
                var topicsByCategory = new Dictionary<Guid, List<Guid>>();
                var topics = await _mediHub4RumContext.Topic
                    .Where(t => categoryIds.Contains(t.Category_Id))
                    .Select(t => new { t.Category_Id, t.Id })
                    .ToListAsync();

                foreach (var categoryId in categoryIds)
                {
                    var topicIdsForCategory = topics
                        .Where(t => t.Category_Id == categoryId)
                        .Select(t => t.Id)
                        .ToList();
                    topicsByCategory.Add(categoryId, topicIdsForCategory);
                }

                // Lấy danh sách LogLesson của User trong năm hiện tại
                var userLogLessons = await _logActionContext.LogLesson
                    .Where(ll => ll.DateAccess >= firstDayOfMonth && ll.DateAccess <= now)
                    .ToListAsync();

                // Kiểm tra người dùng nào đã hoàn thành ít nhất một khóa học không cấp chứng chỉ
                var completedUsers = new HashSet<Guid>();
                foreach (var category in topicsByCategory)
                {
                    var topicIdsForCategory = category.Value;
                    var usersCompletedCategory = userLogLessons
                        .Where(log => topicIdsForCategory.Contains(log.TopicId))
                        .GroupBy(log => log.UserId)
                        .Where(group => topicIdsForCategory.All(topicId => group.Select(log => log.TopicId).Contains(topicId)))
                        .Select(group => group.Key);

                    foreach (var userId in usersCompletedCategory)
                    {
                        completedUsers.Add(userId);
                    }
                }

                model.FinishTime = completedUsers.Count;


                model.Register = await _logActionContext.LogLesson
                    .Where(x => lessonIds.Contains(x.TopicId)
                             && firstDayOfMonth <= x.DateAccess && x.DateAccess <= now)
                    .Select(x => x.UserId)
                    .Distinct()
                    .CountAsync();

                model.FinishCourse = await _mediHub4RumContext.SponsorHubCourseReport
                        .Where(e => e.Category.HubCourse != null &&
                                      e.Category.HubCourse.SponsorId == sponsorId &&
                                      e.Category.HubCourse.CourseType == 0 && currentYear == e.Year && currentMonth == e.Month )
                        .SumAsync(e => e.FinishCourse);
                return model;
            }
            catch(Exception ex)
            {
                return new CertificateModel();
            }
        }
        private async Task<CertificateModel> GetTotalCertificatesCategoryForYearAsync(Guid sponsorId)
        {
            
            var model = new CertificateModel();
            var now = DateTime.Now;
            var firstDayOfYear = new DateTime(now.Year, 1, 1);
            int currentYear = now.Year;
            try
            {
                var categoryIds = await _mediHub4RumContext.SponsorHubCourse
                   .Where(shc => shc.SponsorId == sponsorId && shc.Category.HubCourse.CourseType == 0)
                   .Select(shc => shc.CategoryId)
                   .ToListAsync();
                var lessonIds = await _mediHub4RumContext.Topic
                .Where(t => categoryIds.Contains(t.Category_Id))
                .Select(t => t.Id)
                .ToListAsync();

                var topicsByCategory = new Dictionary<Guid, List<Guid>>();
                var topics = await _mediHub4RumContext.Topic
                    .Where(t => categoryIds.Contains(t.Category_Id))
                    .Select(t => new { t.Category_Id, t.Id })
                    .ToListAsync();

                foreach (var categoryId in categoryIds)
                {
                    var topicIdsForCategory = topics
                        .Where(t => t.Category_Id == categoryId)
                        .Select(t => t.Id)
                        .ToList();
                    topicsByCategory.Add(categoryId, topicIdsForCategory);
                }

                // Lấy danh sách LogLesson của User trong năm hiện tại
                var userLogLessons = await _logActionContext.LogLesson
                    .Where(ll => ll.DateAccess >= firstDayOfYear && ll.DateAccess <= now)
                    .ToListAsync();

                // Kiểm tra người dùng nào đã hoàn thành ít nhất một khóa học không cấp chứng chỉ
                var completedUsers = new HashSet<Guid>();
                foreach (var category in topicsByCategory)
                {
                    var topicIdsForCategory = category.Value;
                    var usersCompletedCategory = userLogLessons
                        .Where(log => topicIdsForCategory.Contains(log.TopicId))
                        .GroupBy(log => log.UserId)
                        .Where(group => topicIdsForCategory.All(topicId => group.Select(log => log.TopicId).Contains(topicId)))
                        .Select(group => group.Key);

                    foreach (var userId in usersCompletedCategory)
                    {
                        completedUsers.Add(userId);
                    }
                }

                model.FinishTime = completedUsers.Count;

                model.Register = await _logActionContext.LogLesson
                    .Where(x => lessonIds.Contains(x.TopicId)
                             && firstDayOfYear <= x.DateAccess && x.DateAccess <= now)
                    .Select(x => x.UserId)
                    .Distinct()
                    .CountAsync();

               
                // Đếm số lượng UserId duy nhất
              

                model.FinishCourse = await _mediHub4RumContext.SponsorHubCourseReport
                        .Where(cfr => cfr.Category.HubCourse != null &&
                                      cfr.Category.HubCourse.SponsorId == sponsorId &&
                                      cfr.Category.HubCourse.CourseType == 0 &&
                                      currentYear == cfr.Year)
                        .SumAsync(cfr => cfr.FinishCourse);
                return model;
            }
            catch (Exception ex)
            {
                return new CertificateModel();
            }
        }
        private async Task<NonCertificateModel> GetTotalNonCertificatesCategoryForMonthAsync(Guid sponsorId)
        {
            var model = new NonCertificateModel();
            var now = DateTime.Now;
            var firstDayOfMonth = new DateTime(now.Year, now.Month, 1);
            int currentYear = now.Year;
            int currentMonth = now.Month;

            try
            {
                var categoryIds = await _mediHub4RumContext.SponsorHubCourse
                  .Where(shc => shc.SponsorId == sponsorId && shc.Category.HubCourse.CourseType == 1)
                  .Select(shc => shc.CategoryId)
                  .ToListAsync();

                var lessonIds = await _mediHub4RumContext.Topic
               .Where(t => categoryIds.Contains(t.Category_Id))
               .Select(t => t.Id)
               .ToListAsync();

                var topicsByCategory = new Dictionary<Guid, List<Guid>>();
                var topics = await _mediHub4RumContext.Topic
                    .Where(t => categoryIds.Contains(t.Category_Id))
                    .Select(t => new { t.Category_Id, t.Id })
                    .ToListAsync();

                foreach (var categoryId in categoryIds)
                {
                    var topicIdsForCategory = topics
                        .Where(t => t.Category_Id == categoryId)
                        .Select(t => t.Id)
                        .ToList();
                    topicsByCategory.Add(categoryId, topicIdsForCategory);
                }

                // Lấy danh sách LogLesson của User trong năm hiện tại
                var userLogLessons = await _logActionContext.LogLesson
                    .Where(ll => ll.DateAccess >= firstDayOfMonth && ll.DateAccess <= now)
                    .ToListAsync();

                // Kiểm tra người dùng nào đã hoàn thành ít nhất một khóa học không cấp chứng chỉ
                var completedUsers = new HashSet<Guid>();
                foreach (var category in topicsByCategory)
                {
                    var topicIdsForCategory = category.Value;
                    var usersCompletedCategory = userLogLessons
                        .Where(log => topicIdsForCategory.Contains(log.TopicId))
                        .GroupBy(log => log.UserId)
                        .Where(group => topicIdsForCategory.All(topicId => group.Select(log => log.TopicId).Contains(topicId)))
                        .Select(group => group.Key);

                    foreach (var userId in usersCompletedCategory)
                    {
                        completedUsers.Add(userId);
                    }
                }

                model.FinishTime = completedUsers.Count;

                model.Study = await _logActionContext.LogLesson
                    .Where(x => lessonIds.Contains(x.TopicId)
                             && firstDayOfMonth <= x.DateAccess && x.DateAccess <= now)
                    .Select(x => x.UserId)
                    .Distinct()
                    .CountAsync();

                model.FinishCourse = await _mediHub4RumContext.SponsorHubCourseReport
                       .Where(cfr => cfr.Category.HubCourse != null &&
                                     cfr.Category.HubCourse.SponsorId == sponsorId &&
                                     cfr.Category.HubCourse.CourseType == 1 && currentMonth == cfr.Month
                                     && currentYear == cfr.Year)
                       .SumAsync(cfr => cfr.FinishCourse);
                return model;
            }
            catch (Exception ex)
            {
                return new NonCertificateModel();
            }
        }
        private async Task<NonCertificateModel> GetTotalNonCertificatesCategoryForYearAsync(Guid sponsorId)
        {
            var model = new NonCertificateModel();
            var now = DateTime.Now;
            var firstDayOfYear = new DateTime(now.Year, 1, 1);
            int currentYear = now.Year;
            try
            {
                var categoryIds = await _mediHub4RumContext.SponsorHubCourse
                 .Where(shc => shc.SponsorId == sponsorId && shc.Category.HubCourse.CourseType == 1)
                 .Select(shc => shc.CategoryId)
                 .ToListAsync();

                var lessonIds = await _mediHub4RumContext.Topic
               .Where(t => categoryIds.Contains(t.Category_Id))
               .Select(t => t.Id)
               .ToListAsync();

                var topicsByCategory = new Dictionary<Guid, List<Guid>>();
                var topics = await _mediHub4RumContext.Topic
                    .Where(t => categoryIds.Contains(t.Category_Id))
                    .Select(t => new { t.Category_Id, t.Id })
                    .ToListAsync();

                foreach (var categoryId in categoryIds)
                {
                    var topicIdsForCategory = topics
                        .Where(t => t.Category_Id == categoryId)
                        .Select(t => t.Id)
                        .ToList();
                    topicsByCategory.Add(categoryId, topicIdsForCategory);
                }

                // Lấy danh sách LogLesson của User trong năm hiện tại
                var userLogLessons = await _logActionContext.LogLesson
                    .Where(ll => ll.DateAccess >= firstDayOfYear && ll.DateAccess <= now)
                    .ToListAsync();

                // Kiểm tra người dùng nào đã hoàn thành ít nhất một khóa học không cấp chứng chỉ
                var completedUsers = new HashSet<Guid>();
                foreach (var category in topicsByCategory)
                {
                    var topicIdsForCategory = category.Value;
                    var usersCompletedCategory = userLogLessons
                        .Where(log => topicIdsForCategory.Contains(log.TopicId))
                        .GroupBy(log => log.UserId)
                        .Where(group => topicIdsForCategory.All(topicId => group.Select(log => log.TopicId).Contains(topicId)))
                        .Select(group => group.Key);

                    foreach (var userId in usersCompletedCategory)
                    {
                        completedUsers.Add(userId);
                    }
                }

                model.FinishTime = completedUsers.Count;

                model.Study = await _logActionContext.LogLesson
                    .Where(x => lessonIds.Contains(x.TopicId) 
                             && firstDayOfYear <= x.DateAccess && x.DateAccess <= now)
                    .Select(x => x.UserId)
                    .Distinct()
                    .CountAsync();

                model.FinishCourse = await _mediHub4RumContext.SponsorHubCourseReport
                       .Where(cfr => cfr.Category.HubCourse != null &&
                                     cfr.Category.HubCourse.SponsorId == sponsorId &&
                                     cfr.Category.HubCourse.CourseType == 1 &&
                                     currentYear == cfr.Year)
                       .SumAsync(cfr => cfr.FinishCourse);
                return model;
            }
            catch (Exception ex)
            {
                return new NonCertificateModel();
            }
        }
        private async Task<int> GetUserStudyForMonthAsync(Guid sponsorId)
        {
            var now = DateTime.Now;
            var firstDayOfMonth = new DateTime(now.Year, now.Month, 1);

            try
            {
                

                // Lấy danh sách CategoryId của Sponsor
                var categoryIds = await _mediHub4RumContext.SponsorHubCourse
                    .Where(shc => shc.SponsorId == sponsorId &&
                (shc.Category.HubCourse.CourseType == 0 || shc.CourseType == 1))
                    .Select(shc => shc.CategoryId)
                    .ToListAsync();
                var lessonIds = await _mediHub4RumContext.Topic
                 .Where(t => categoryIds.Contains(t.Category_Id))
                 .Select(t => t.Id)
                 .ToListAsync();
                // Đếm số lượng UserId duy nhất
                return await _logActionContext.LogLesson
                    .Where(x => lessonIds.Contains(x.TopicId) 
                             && firstDayOfMonth <= x.DateAccess && x.DateAccess <= now)
                    .Select(x => x.UserId)
                    .Distinct()
                    .CountAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in GetUserStudyForMonthAsync for SponsorId: {sponsorId}");
                throw;
            }
        }
        private async Task<int> GetUserStudyForYearAsync(Guid sponsorId)
        {
            var now = DateTime.Now;
            var firstDayOfYear = new DateTime(now.Year, 1, 1);

            try
            {
                // Lấy danh sách CategoryId của Sponsor
                var categoryIds = await _mediHub4RumContext.SponsorHubCourse
                    .Where(shc => shc.SponsorId == sponsorId &&
                (shc.Category.HubCourse.CourseType == 0 || shc.CourseType == 1))
                    .Select(shc => shc.CategoryId)
                    .ToListAsync();
                var lessonIds = await _mediHub4RumContext.Topic
                 .Where(t => categoryIds.Contains(t.Category_Id))
                 .Select(t => t.Id)
                 .ToListAsync();
                // Đếm số lượng UserId duy nhất
                return await _logActionContext.LogLesson
                    .Where(x => lessonIds.Contains(x.TopicId) 
                             && firstDayOfYear <= x.DateAccess && x.DateAccess <= now)
                    .Select( x => x.UserId  )
                    .Distinct()
                    .CountAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in GetUserStudyForYearAsync for SponsorId: {sponsorId}");
                throw;
            }
        }
        private async Task<int> GetTotalUsersCompletedCoursesAsync(DateTime startDate, DateTime endDate, Guid sponsorId)
        {
            //  Lấy danh sách CategoryId của Sponsor
            var categoryIds = await _mediHub4RumContext.SponsorHubCourse
                .Where(shc => shc.SponsorId == sponsorId &&
                              (shc.Category.HubCourse.CourseType == 0 || shc.CourseType == 1)) 
                .Select(shc => shc.CategoryId)
                .ToListAsync();

           
            if (!categoryIds.Any())
            {
                return 0; 
            }

            // Lấy danh sách TopicId theo từng CategoryId
            var topicsByCategory = new Dictionary<Guid, List<Guid>>();
            try
            {
                var topics = await _mediHub4RumContext.Topic
                    .Where(t => categoryIds.Contains(t.Category_Id))
                    .Select(t => new { t.Category_Id, t.Id })
                    .ToListAsync();

                foreach (var categoryId in categoryIds)
                {
                    var topicIdsForCategory = topics
                        .Where(t => t.Category_Id == categoryId)
                        .Select(t => t.Id)
                        .ToList();
                    topicsByCategory.Add(categoryId, topicIdsForCategory);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in Step 2: {ex.Message}");
            }

            // Lấy danh sách LogLesson của User trong khoảng thời gian xác định
            var userLogLessons = await _logActionContext.LogLesson
                .Where(ll => ll.DateAccess >= startDate && ll.DateAccess <= endDate)
                .ToListAsync();

            //  Kiểm tra người dùng nào đã click vào đủ tất cả các Topic của ít nhất một Category
            var completedUsers = new List<Guid>(); 
            foreach (var category in topicsByCategory)
            {
                var topicIdsForCategory = category.Value; // Danh sách TopicId cho Category hiện tại
                                                         
                var usersCompletedCategory = userLogLessons
                    .Where(log => topicIdsForCategory.Contains(log.TopicId)) // Chỉ lấy LogLesson cho các Topic trong Category này
                    .GroupBy(log => log.UserId) 
                    .Where(group => topicIdsForCategory.All(topicId => group.Select(log => log.TopicId).Contains(topicId))) // Kiểm tra xem User đã hoàn thành đủ tất cả các Topic chưa
                    .Select(group => group.Key)
                    .ToList();
                completedUsers.AddRange(usersCompletedCategory); // Thêm danh sách UserId hoàn thành vào danh sách chính
            }

            
            return completedUsers.Distinct().Count(); 
        }








        private async Task<int> GetTotalUsersGetCertificatesAsync(DateTime startDate, DateTime endDate, Guid sponsorId)
        {
            // Lấy danh sách CategoryId của Sponsor
            var categoryIds = await _mediHub4RumContext.SponsorHubCourse
                .Where(shc => shc.SponsorId == sponsorId
                &&  shc.Category.HubCourse.CourseType == 0)
                .Select(shc => shc.CategoryId)
                .ToListAsync();

            return await _mediHub4RumContext.SponsorHubCourseFinish
                .Where(cfr => cfr.FinishDate >= startDate
                           && cfr.FinishDate <= endDate
                           && categoryIds.Contains(cfr.CategoryId)
                           && cfr.IsPassed == true)
                .Select(cfr => cfr.UserId)
                .Distinct()
                .CountAsync();
        }
    }
}