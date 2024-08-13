using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Medihub4rumEntities
{
    public class SponsorHubCourseReport
    {
        public Guid  SponsorId { get; set; }
        public Guid CategoryId { get; set; }

        public int FinishAtleastOneLesson { get; set; }

        public int FinishAtleasFiftyPercentCourse { get; set; }
        public int FinishCourse {  get; set; }
        public int PassCourse { get; set; }
        public int Year {  get; set; }
        public int Month { get; set; }

        [ForeignKey("CategoryId")]
        public Category Category { get; set; }
    }
}
