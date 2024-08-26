using System;
using System.Collections.Generic;

namespace PMCNet8.Models
{
    public class SurveyViewModel
    {
        public long SurveyId { get; set; }
        public string SurveyName { get; set; }
        public string CourseName { get; set; }
        public string CourseType { get; set; }
        public string SurveyType { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int TotalParticipants { get; set; }
        public List<QuestionViewModel> Questions { get; set; }
    }
}