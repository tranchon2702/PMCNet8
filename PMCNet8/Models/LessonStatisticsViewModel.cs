

namespace PMCNet8.Models
{
    public class LessonStatisticsViewModel
    {
        public string LessonName { get; set; }
        public string CourseName { get; set; }
        public ChartLessonViewModel ChartData { get; set; }
        public List<LessonUserActivityViewModel> TableData { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public List<ProductListItem> Products { get; set; }
        public List<LessonListItem> Lessons { get; set; }
    }
}
