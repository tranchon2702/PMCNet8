namespace PMCNet8.Models
{
    public class ChartDataViewModel
    {
        public int? TopicId { get; set; }
        public int? Joins { get; set; }
        public int? CompleteTest { get; set; }
        public int? FailedTest { get; set; }
        public string? Lesson { get;  set; }
        public int? CompletedLesson { get; set; } 
        public int? TotalQuestions { get; set; }
    }
}
