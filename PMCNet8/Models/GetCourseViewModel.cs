
namespace PMCNet8.Models
{
    public class GetCourseViewModel
    {
        public string Name { get; set; }
        public string? Description { get; set; }
        public int Type { get; set; }
        public int Quantity { get; set; }
        public object? PieData { get; set; }
        public int TotalKeys { get; set; }
        public int UsedKeys { get; set; }
        public int UnusedKeys { get; set; }
        public Guid Id { get;  set; }
    }

    
}
