using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Data.Data;
using Data.Medihub4rumEntities;
using System;
using System.Linq;
using System.Threading.Tasks;
using PMCNet8.Models;


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

        public IActionResult Index()
        {
            HomeViewModel model = new HomeViewModel();
            return View(model);
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

            try
            {

                model.Watched = await _mediHub4RumContext.MediHubScQuizResult
                    .Where(e => e.Topic.Category.HubCourse != null && e.Topic.Category.HubCourse.SponsorId == sponsorId
                    && firstDayOfMonth <= e.DateAdded && e.DateAdded <= now)
                    .Select(e => e.KeyAppActive)
                    .Distinct()
                    .CountAsync();


                model.Finish = await _mediHub4RumContext.SponsorHubCourseFinish
                       .Where(cfr => cfr.Category.HubCourse != null &&
                                     cfr.Category.HubCourse.SponsorId == sponsorId &&
                                     cfr.Category.HubCourse.CourseType == 1 &&
                                     firstDayOfMonth <= cfr.FinishDate.Date &&
                                     cfr.FinishDate.Date <= now.Date)
                       .Select(cfr => cfr.UserId)
                       .Distinct()
                       .CountAsync();
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

            try
            {

                model.Watched = await _mediHub4RumContext.MediHubScQuizResult
                    .Where(e => e.Topic.Category.HubCourse != null && e.Topic.Category.HubCourse.SponsorId == sponsorId
                    && firstDayOfYear <= e.DateAdded && e.DateAdded <= now)
                    .Select(e => e.KeyAppActive)
                    .Distinct()
                    .CountAsync();

                model.Finish = await _mediHub4RumContext.SponsorHubCourseFinish
                        .Where(cfr => cfr.Category.HubCourse != null &&
                                      cfr.Category.HubCourse.SponsorId == sponsorId &&
                                      cfr.Category.HubCourse.CourseType == 1 &&
                                      firstDayOfYear <= cfr.FinishDate.Date &&
                                      cfr.FinishDate.Date <= now.Date)
                        .Select(cfr => cfr.UserId)
                        .Distinct()
                        .CountAsync();
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
                   .Where(shc => shc.SponsorId == sponsorId)
                   .Select(shc => shc.CategoryId)
                   .ToListAsync();
                model.FinishTime = await _mediHub4RumContext.SponsorHubCourseReport
                   .Where(x => x.Category.HubCourse != null
                            && x.Category.HubCourse.SponsorId == sponsorId
                            && x.Category.HubCourse.CourseType == 0
                            && x.Year == currentYear
                            && x.Month == currentMonth)
                   .SumAsync(x => x.FinishAtleasFiftyPercentCourse);

                model.Register = await _mediHubSCAppContext.CPECourseActive
                  .Where(e => categoryIds.Any(x => x == e.CategoryId && e.Category.HubCourse.CourseType == 0)
                  && firstDayOfMonth <= e.DateCreated && e.DateCreated <= now)
                  .Select(e => e.KeyCodeActive)
                  .Distinct()
                  .CountAsync();

                model.FinishCourse = await _mediHub4RumContext.SponsorHubCourseReport
                        .Where(e => e.Category.HubCourse != null &&
                                      e.Category.HubCourse.SponsorId == sponsorId &&
                                      e.Category.HubCourse.CourseType == 0 &&
                                      currentMonth == e.Month)
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
                   .Where(shc => shc.SponsorId == sponsorId)
                   .Select(shc => shc.CategoryId)
                   .ToListAsync();

                model.FinishTime = await _mediHub4RumContext.SponsorHubCourseReport
                    .Where(x => x.Category.HubCourse != null
                             && x.Category.HubCourse.SponsorId == sponsorId
                             && x.Category.HubCourse.CourseType == 0
                             && x.Year == currentYear)
                    .SumAsync(x => x.FinishAtleasFiftyPercentCourse);

                model.Register = await _mediHubSCAppContext.CPECourseActive
                    .Where(e => categoryIds.Any(x => x == e.CategoryId) && e.Category.HubCourse.CourseType == 0
                    && firstDayOfYear <= e.DateCreated && e.DateCreated <= now)
                    .Select(e => e.KeyCodeActive)
                    .Distinct()
                    .CountAsync();

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
                    .Where(shc => shc.SponsorId == sponsorId)
                    .Select(shc => shc.CategoryId)
                    .ToListAsync();

                model.FinishTime = await _mediHub4RumContext.SponsorHubCourseReport
                    .Where(x => x.Category.HubCourse != null
                             && x.Category.HubCourse.SponsorId == sponsorId
                             && x.Category.HubCourse.CourseType == 2
                             && x.Year == currentYear
                             && x.Month == currentMonth)
                    .SumAsync(x => x.FinishAtleasFiftyPercentCourse);

                model.Study = await _mediHub4RumContext.SponsorHubCourseReport
                    .Where(x => x.Category.HubCourse != null
                             && x.Category.HubCourse.SponsorId == sponsorId
                             && x.Year == currentYear)
                    .SumAsync(x => x.FinishAtleastOneLesson);

                model.FinishCourse = await _mediHub4RumContext.SponsorHubCourseFinish
                        .Where(cfr => cfr.Category.HubCourse != null &&
                                      cfr.Category.HubCourse.SponsorId == sponsorId &&
                                      cfr.Category.HubCourse.CourseType == 2 &&
                                      firstDayOfMonth <= cfr.FinishDate &&
                                      cfr.FinishDate <= now)
                        .CountAsync();
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
                 .Where(shc => shc.SponsorId == sponsorId && shc.Category.HubCourse.CourseType == 2)
                 .Select(shc => shc.CategoryId)
                 .ToListAsync();

                model.FinishTime = await _mediHub4RumContext.SponsorHubCourseReport
                    .Where(x => x.Category.HubCourse != null
                             && x.Category.HubCourse.SponsorId == sponsorId
                             && x.Category.HubCourse.CourseType == 2
                             && x.Year == currentYear)
                    .SumAsync(x => x.FinishAtleasFiftyPercentCourse);

                model.Study = await _mediHub4RumContext.SponsorHubCourseReport
                    .Where(x => x.Category.HubCourse != null
                             && x.Category.HubCourse.SponsorId == sponsorId
                             && x.Year == currentYear)
                    .SumAsync(x => x.FinishAtleastOneLesson);

                model.FinishCourse = await _mediHub4RumContext.SponsorHubCourseFinish
                        .Where(cfr => cfr.Category.HubCourse != null &&
                                      cfr.Category.HubCourse.SponsorId == sponsorId &&
                                      cfr.Category.HubCourse.CourseType == 2 &&
                                      firstDayOfYear <= cfr.FinishDate &&
                                      cfr.FinishDate <= now)
                        .CountAsync();
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
            int currentYear = now.Year;
            int currentMonth = now.Month;

            try
            {

                
                // Lấy danh sách CategoryId của Sponsor
                var categoryIds = await _mediHub4RumContext.SponsorHubCourse
                    .Where(shc => shc.SponsorId == sponsorId)
                    .Select(shc => shc.CategoryId)
                    .ToListAsync();
                var lessonIds = await _mediHub4RumContext.Topic
                 .Where(t => categoryIds.Contains(t.Category_Id))
                 .Select(t => t.Id)
                 .ToListAsync();
                // Đếm số lượng UserId duy nhất
                return await _logActionContext.LogLesson
                    .Where(x => lessonIds.Contains(x.TopicId)
                             && x.DateAccess.Year == currentYear
                             && x.DateAccess.Month == currentMonth)
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
            int currentYear = now.Year;

            try
            {
                // Lấy danh sách CategoryId của Sponsor
                var categoryIds = await _mediHub4RumContext.SponsorHubCourse
                    .Where(shc => shc.SponsorId == sponsorId)
                    .Select(shc => shc.CategoryId)
                    .ToListAsync();
                var lessonIds = await _mediHub4RumContext.Topic
                 .Where(t => categoryIds.Contains(t.Category_Id))
                 .Select(t => t.Id)
                 .ToListAsync();
                // Đếm số lượng UserId duy nhất
                return await _logActionContext.LogLesson
                    .Where(x => lessonIds.Contains(x.TopicId)
                             && x.DateAccess.Year == currentYear)
                    .Select(x => x.UserId)
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
            // Lấy danh sách CategoryId của Sponsor
            var categoryIds = await _mediHub4RumContext.SponsorHubCourse
                .Where(shc => shc.SponsorId == sponsorId)
                .Select(shc => shc.CategoryId)
                .ToListAsync();

            return await _mediHub4RumContext.SponsorHubCourseFinish
                .Where(cfr =>  cfr.FinishDate >= startDate
                           && cfr.FinishDate <= endDate
                           && categoryIds.Contains(cfr.CategoryId))
                .Select(cfr => cfr.UserId)
                .Distinct()
                .CountAsync();
        }
        private async Task<int> GetTotalUsersGetCertificatesAsync(DateTime startDate, DateTime endDate, Guid sponsorId)
        {
            // Lấy danh sách CategoryId của Sponsor
            var categoryIds = await _mediHub4RumContext.SponsorHubCourse
                .Where(shc => shc.SponsorId == sponsorId)
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