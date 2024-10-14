using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Data.Medihub4rumEntities
{
    public class DmWard
    {
        [Key]
        public Guid Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public Guid? DmDistricId { get; set; }

        public DmDistrict DmDistric { get; set; }
    }
}
