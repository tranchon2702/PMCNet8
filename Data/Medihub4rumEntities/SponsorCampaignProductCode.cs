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
    public class SponsorCampaignProductCode
    {
        [Key]
        public string Code { get; set; }

        public Guid CampaignId { get; set; }

        [AllowNull]
        public string? QRBase64 { get; set; }

        [ForeignKey("CampaignId")]
        public virtual SponsorCampaign Campaign { get; set; }

        public virtual SponsorCampaignProductScan? ProductScan { get; set; }
    }
}
