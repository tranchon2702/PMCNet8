
namespace PMCNet8.Models
{
    public class LessonListItem
    {
        public Guid Id { get; internal set; }
        public string Name { get; internal set; }
        public string CourseName { get; internal set; }
        public Guid CourseId { get; internal set; }
    }
}
