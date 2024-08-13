using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Medihub4rumEntities
{
    [PrimaryKey(nameof(SponsorId), nameof(CategoryId))]
    public class SponsorHubCourse
    {
        
        [Column(Order = 0)]
        public Guid SponsorId { get; set; }
        
        [Column(Order = 1)]
        public Guid CategoryId { get; set; }
        public int CourseType { get; set; }
        public bool RequiredCode { get; set; }
        public int TargetJoin {  get; set; }
        public int TargetFinish { get; set; }
        public DateTime? TargetStartDate { get; set; }
        public DateTime? TargetEndDate { get; set; }
        public int Order { get; set; }

        [ForeignKey("CategoryId")]
        public Category Category { get; set; }

        [ForeignKey("SponsorId")]
        public SponsorHub HubCourse { get; set; }
    }
}
