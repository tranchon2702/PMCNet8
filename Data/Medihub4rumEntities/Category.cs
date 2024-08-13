using Microsoft.EntityFrameworkCore.Metadata;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Medihub4rumEntities
{
    public class Category
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool IsLocked { get; set; }
        public bool ModerateTopics { get; set; }
        public bool ModeratePosts { get; set; }
        public int SortOrder { get; set; }
        public DateTime DateCreated { get; set; }
        public string Slug { get; set; }
        public string PageTitle { get;}
        public string Path { get; set; }
        public string MetaDescription { get; set; }
        public string Colour { get; set; }
        public string Image {  get; set; } 
        public Guid Category_Id { get; set; }
        public bool IsMediHubSc { get; set;}
        public bool iStepD { get; set; }
        public string Sponsor { get; set;}
        public int CourseType { get; set; }
        public int TargetJoin { get;}
        public int TargetFinish { get; set; }
        public DateTime TargetStartDate { get; set; }
        public DateTime TargetEndDate { get; set; }

        public SponsorHubCourse? HubCourse { get; set; }
        public SponsorHubCourseFinish? FinishReport { get; set; }

        public ICollection<CategorySurvey> CategorySurvies { get; set; }
        public ICollection<Topic> Topics { get; set; }
    }
}
