using Data.Data;
using Data.MedihubSCAppEntities;
using Data.Medihub4rumEntities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PMCNet8.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Globalization;

namespace PMCNet8.Controllers
{
    public class CourseSurveyController : Controller
    {
        private readonly LogActionDbContext _logActionDbContext;
        private readonly Medihub4rumDbContext _mediHub4RumContext;
        private readonly MedihubSCAppDbContext _mediHubSCAppContext;
        private readonly ILogger<CourseSurveyController> _logger;

        public CourseSurveyController(LogActionDbContext logActionDbContext, Medihub4rumDbContext mediHub4RumContext, MedihubSCAppDbContext mediHubSCAppContext, ILogger<CourseSurveyController> logger)
        {
            _logActionDbContext = logActionDbContext;
            _mediHub4RumContext = mediHub4RumContext;
            _mediHubSCAppContext = mediHubSCAppContext;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var sponsorId = Guid.Parse(HttpContext.Session.GetString("SponsorId"));

                var courses = await _mediHub4RumContext.SponsorHubCourse
                    .Where(shc => shc.SponsorId == sponsorId)
                    .Select(shc => new SelectListItem
                    {
                        Value = shc.CategoryId.ToString(),
                        Text = shc.Category.Name
                    })
                    .ToListAsync();

                ViewBag.Courses = courses;

                if (courses.Count == 0)
                {
                    ViewBag.ErrorMessage = "Không có khóa học nào được tìm thấy.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách khóa học");
                ViewBag.ErrorMessage = "Đã xảy ra lỗi khi tải danh sách khóa học.";
                ViewBag.Courses = new List<SelectListItem>();
            }

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetSurveyData(Guid courseId, int surveyType, string startDate, string endDate)
        {
            try
            {
                _logger.LogInformation($"Đang lấy dữ liệu khảo sát: CourseId={courseId}, SurveyType={surveyType}, StartDate={startDate}, EndDate={endDate}");

                DateTime parsedStartDate = DateTime.ParseExact(startDate, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                DateTime parsedEndDate = DateTime.ParseExact(endDate, "dd/MM/yyyy", CultureInfo.InvariantCulture);

                var categorySurvey = await _mediHub4RumContext.CategorySurvey
                    .FirstOrDefaultAsync(cs => cs.CategoryId == courseId && cs.Type == surveyType);

                if (categorySurvey == null)
                {
                    return PartialView("_CourseSurvey", new SurveyViewModel { Questions = new List<QuestionViewModel>() });
                }

                var surveyData = await GetSurveyViewModel(courseId, surveyType, parsedStartDate, parsedEndDate);
                surveyData.Questions = surveyData.Questions.OrderBy(q => q.Order).ToList();
                return PartialView("_CourseSurvey", surveyData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi lấy dữ liệu khảo sát: CourseId={courseId}, SurveyType={surveyType}, StartDate={startDate}, EndDate={endDate}");
                return StatusCode(500, "Đã xảy ra lỗi khi xử lý yêu cầu của bạn: " + ex.Message);
            }
        }

        private async Task<SurveyViewModel> GetSurveyViewModel(Guid courseId, int surveyType, DateTime parsedStartDate, DateTime parsedEndDate)
        {
            try
            {
                var categorySurvey = await _mediHub4RumContext.CategorySurvey
                    .FirstOrDefaultAsync(cs => cs.CategoryId == courseId && cs.Type == surveyType);

                if (categorySurvey == null)
                {
                    throw new Exception("Không tìm thấy khảo sát cho khóa học này.");
                }

                var survey = await _mediHubSCAppContext.PharmacomSurvey
                    .FirstOrDefaultAsync(s => s.Id == categorySurvey.SurveyId);

                if (survey == null)
                {
                    throw new Exception("Không tìm thấy thông tin khảo sát.");
                }

                var courseInfo = await _mediHub4RumContext.SponsorHubCourse.AsNoTracking()
                    .Include(e => e.Category)
                    .FirstOrDefaultAsync(shc => shc.CategoryId == courseId);

                if (courseInfo == null)
                {
                    throw new Exception("Không tìm thấy thông tin khóa học.");
                }

                var surveyViewModel = new SurveyViewModel
                {
                    SurveyId = survey.Id,
                    SurveyName = survey.Name,
                    CourseName = courseInfo.Category.Name,
                    SurveyType = surveyType == 0 ? "Trước khóa học" : "Sau khóa học",
                    StartDate = parsedStartDate,
                    EndDate = parsedEndDate,
                    CourseType = GetCourseTypeName(courseInfo.CourseType),
                    TotalParticipants = await GetTotalParticipants(survey.Id, parsedStartDate, parsedEndDate),
                    Questions = await GetQuestionViewModels(survey.Id, parsedStartDate, parsedEndDate)
                };

                return surveyViewModel;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo SurveyViewModel");
                return new SurveyViewModel();
            }
        }

        private string GetCourseTypeName(int courseType)
        {
            return courseType switch
            {
                0 => "Khóa học có cấp chứng chỉ",
                1 => "Khóa học cập nhật kiến thức",
                2 => "Khóa học không cấp chứng chỉ",
                _ => "Không xác định"
            };
        }

        private async Task<int> GetTotalParticipants(long surveyId, DateTime parsedStartDate, DateTime parsedEndDate)
        {
            return await _mediHubSCAppContext.PharmacomSurveyVote
                .Where(psv => psv.PharmacomSurveyId == surveyId &&
                              psv.DateCreated >= parsedStartDate &&
                              psv.DateCreated <= parsedEndDate)
                .Select(psv => psv.KeyCodeActive)
                .Distinct()
                .CountAsync();
        }

        private async Task<List<QuestionViewModel>> GetQuestionViewModels(long surveyId, DateTime parsedStartDate, DateTime parsedEndDate)
        {
            var questions = await _mediHubSCAppContext.PharmacomSurveyDetail
                .Where(q => q.PharmacomSurveyId == surveyId)
                .OrderBy(q => q.Order)
                .ToListAsync();

            var answers = await _mediHubSCAppContext.PharmacomSurveyVote
                .Where(psv => psv.PharmacomSurveyId == surveyId &&
                              psv.DateCreated >= parsedStartDate &&
                              psv.DateCreated <= parsedEndDate)
                .ToListAsync();

            var questionViewModels = new List<QuestionViewModel>();

            foreach (var question in questions)
            {
                var options = ConvertSurveyContentToOptions(question.SurveyContent);
                var responses = answers
                    .Where(e => e.PharmacomSurveyDetailId == question.Id)
                    .Select(e => new ResponseViewModel
                    {
                        ResponseId = e.Id,
                        Value = e.Evaluation,
                        CreatedDate = e.DateCreated,
                        KeyCodeActive = e.KeyCodeActive
                    }).ToList();

                var questionView = new QuestionViewModel
                {
                    QuestionId = question.Id,
                    QuestionTitle = question.QuestionTitle,
                    Type = question.Type,
                    Options = options,
                    Responses = responses,
                    Order = question.Order
                };

                switch (question.Type.ToLower())
                {
                    case "star":
                        questionView.Statistics = CalculateStarStatistics(options, responses);
                        break;
                    case "radio":
                    case "checkbox":
                        var (optionCounts, optionPercentages) = CalculateOptionStatistics(options, responses, question.Type);
                        questionView.OptionCounts = optionCounts;
                        questionView.OptionPercentages = optionPercentages;
                        questionView.Statistics = FormatStatistics(optionPercentages);
                        break;
                    case "text":
                    case "input":
                    case "radioinput":
                    case "checkboxinput":
                        // Không cần tính toán thống kê cho các loại câu hỏi này
                        questionView.Statistics = "Không áp dụng";
                        break;
                }

                questionViewModels.Add(questionView);
            }

            return questionViewModels;
        }

        private List<OptionViewModel> ConvertSurveyContentToOptions(string surveyContent)
        {
            try
            {
                var options = JsonConvert.DeserializeObject<List<OptionItem>>(surveyContent ?? "[]");
                return options?.Select(o => new OptionViewModel
                {
                    OptionId = o.Id,
                    Content = o.Name
                }).ToList() ?? new List<OptionViewModel>();
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Lỗi khi chuyển đổi SurveyContent thành Options");
                return new List<OptionViewModel>();
            }
        }

        private string CalculateStarStatistics(List<OptionViewModel> options, List<ResponseViewModel> responses)
        {
            if (responses.Count == 0) return "Không có dữ liệu";

            var maxStars = options.Count;
            var starCounts = new int[maxStars];
            var totalResponses = responses.Count;

            foreach (var response in responses)
            {
                if (int.TryParse(response.Value, out int starValue) && starValue >= 1 && starValue <= maxStars)
                {
                    starCounts[starValue - 1]++;
                }
            }

            var statistics = new List<string>();
            for (int i = 0; i < maxStars; i++)
            {
                var percentage = (starCounts[i] * 100.0) / totalResponses;
                statistics.Add($"{i + 1} sao: {percentage:F1}%");
            }

            return string.Join(", ", statistics);
        }

        private (Dictionary<string, int>, Dictionary<string, double>) CalculateOptionStatistics(List<OptionViewModel> options, List<ResponseViewModel> responses, string questionType)
        {
            var optionCounts = options.ToDictionary(o => o.Content, _ => 0);
            var totalResponses = responses.Count;

            foreach (var response in responses)
            {
                var selectedOptions = questionType.ToLower() == "checkbox"
                    ? JsonConvert.DeserializeObject<List<string>>(response.Value ?? "[]")
                    : new List<string> { response.Value };

                foreach (var selectedOption in selectedOptions)
                {
                    if (optionCounts.ContainsKey(selectedOption))
                    {
                        optionCounts[selectedOption]++;
                    }
                }
            }

            var optionPercentages = optionCounts.ToDictionary(
                kvp => kvp.Key,
                kvp => totalResponses > 0 ? (kvp.Value * 100.0) / totalResponses : 0
            );

            return (optionCounts, optionPercentages);
        }

        private string FormatStatistics(Dictionary<string, double> optionPercentages)
        {
            return string.Join(", ", optionPercentages.Select(kvp => $"{kvp.Key}: {kvp.Value:F1}%"));
        }
    }
}