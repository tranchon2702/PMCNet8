using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Medihub4rumEntities
{
    public class SponsorCampaignProductScan
    {
        [Key]
        public string Code { get; set; }

        public Guid UserId { get; set; }
        public DateTime ScanDate { get; set; }

        [ForeignKey("UserId")]
        public virtual MembershipUser User { get; set; }

        [ForeignKey("Code")]
        public virtual SponsorCampaignProductCode ProductCode { get; set; }
    }
}
