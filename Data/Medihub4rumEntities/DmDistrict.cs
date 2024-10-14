

using System.ComponentModel.DataAnnotations;

namespace Data.Medihub4rumEntities
{
    public class DmDistrict
    {
        public DmDistrict()
        {
            DmWard = new HashSet<DmWard>();
        }

        [Key]
        public Guid Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public Guid? DmProvinceId { get; set; }

        public DmProvince DmProvince { get; set; }
        public ICollection<DmWard> DmWard { get; set; }
    }
}
