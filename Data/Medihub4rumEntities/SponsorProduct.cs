using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Medihub4rumEntities
{
    public class SponsorProduct
    {
        [Key]
        public Guid Id { get; set; }

        public Guid SponsorId { get; set; }

        [Required]
        [MaxLength(300)]
        public string Name { get; set; }

        [AllowNull]
        [MaxLength(300)]
        public string? Image { get; set; }

        [AllowNull]
        [MaxLength(300)]
        public string? Description { get; set; }

        [ForeignKey("SponsorId")]
        public virtual Sponsor Sponsor { get; set; }

        public virtual SponsorCampaign? Campaign { get; set; }
    }
}
