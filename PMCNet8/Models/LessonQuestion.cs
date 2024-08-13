namespace PMCNet8.Models
{
    public class LessonQuestion
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Image { get; set; }
        public int Type { get; set; }
        public List<LessonAnswer> Answers { get; set; }
        public string CorrectAnswer { get; set; }
    }
}
