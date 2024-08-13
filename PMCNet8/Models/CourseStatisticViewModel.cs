using Microsoft.AspNetCore.Mvc.Rendering;

namespace PMCNet8.Models
{
    public class CourseStatisticViewModel
    {
        public Guid SelectedCourseId { get; set; }
        public List<CourseListItems> CourseListItems { get; set; }
        public GetCourseViewModel CourseInfo { get; set; }
        public List<UnregisteredPharmacistViewModel> UnregisteredPharmacists { get; set; }
        public AchieveTargetsViewModel AchieveTargets { get; set; }
    }
}
