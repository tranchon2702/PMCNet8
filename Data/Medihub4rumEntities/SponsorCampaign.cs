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
    public class SponsorCampaign
    {
        [Key]
        public Guid Id { get; set; }

        public Guid ProductId { get; set; }

        [Required]
        [MaxLength(300)]
        public string Name { get; set; }

        [AllowNull]
        [MaxLength(300)]
        public string? Description { get; set; }

        public int Quantity { get; set; }
        public int Point { get; set; }
        public DateTime CreateDate { get; set; }

        [ForeignKey("ProductId")]
        public virtual SponsorProduct Product { get; set; }

        public virtual ICollection<SponsorCampaignProductCode> ProductCodes { get; set; }
    }
}
