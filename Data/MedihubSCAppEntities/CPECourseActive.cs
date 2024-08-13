using Data.Medihub4rumEntities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.MedihubSCAppEntities
{
    public class CPECourseActive
    {
        [Key]
        public long Id { get; set; }

        [AllowNull]
        public string? KeyCourseActive { get; set; }
        [AllowNull]
        public string? Status { get; set; }
        public DateTime DateCreated { get; set; }
        public Guid CategoryId { get; set; }
        [AllowNull]
        public string? KeyCodeActive { get; set; }
        public DateTime? LastUpdated { get; set; }

        [ForeignKey("CategoryId")]
        public Category Category { get; set; }


    }
}
