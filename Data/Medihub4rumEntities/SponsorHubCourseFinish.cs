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
    [PrimaryKey(nameof(CategoryId), nameof(UserId))]
    public class SponsorHubCourseFinish
    {
        [Column(Order = 0)]
        public Guid CategoryId { get; set; }
        [Column(Order = 1)]
        public Guid UserId { get; set; }
        public DateTime FinishDate { get; set; }
        public bool IsPassed { get; set; }

        [ForeignKey("CategoryId")]
        public Category Category { get; set; }

        [ForeignKey("UserId")]
        public MembershipUser User { get; set; }
    }
}
