using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Medihub4rumEntities
{
    public class SponsorHub
    {
        [Key]
        public Guid SponsorId { get; set; }
        public string Name { get; set; }
        public string Image {  get; set; }
        public int Order {  get; set; }

        [ForeignKey("SponsorId")]
        public Sponsor Sponsor { get; set; }
    }
}
