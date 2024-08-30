using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Medihub4rumEntities
{
    [PrimaryKey(nameof(SponsorId), nameof(UserId))]
    public class SponsorUser
    {
      
        [Column(Order = 0)]
        public Guid SponsorId { get; set; }

       
        [Column(Order = 1)]
        public Guid UserId { get; set; }

        [ForeignKey("SponsorId")]
        public virtual Sponsor Sponsor { get; set; }

        [ForeignKey("UserId")]
        public virtual UserAccount Account { get; set; }

    }
}
