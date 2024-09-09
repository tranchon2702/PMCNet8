using Data.Medihub4rumEntities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.LogActionEntities
{
    public class LogClickCourse
    {
        [Key]
        public int Id { get; set; }
        public DateTime CreatedDate { get; set; }
        public Guid CourseId { get; set; }
        public Guid UserId { get; set; }    
        public string Type { get; set; }
        public string OS { get; set; }

        [ForeignKey("CourseId")]
        public Category Category { get; set; }
    }
}
