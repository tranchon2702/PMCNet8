using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Medihub4rumEntities
{
    public class DmProvince
    {
        public DmProvince()
        {
            DmDistrict = new HashSet<DmDistrict>();
        }
        [Key]
        public Guid Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public bool IsActivity { get; set; }

        public ICollection<DmDistrict> DmDistrict { get; set; }
    }
}
