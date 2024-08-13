using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.LogActionEntities
{
    public class LogClickCourse
    {
        public int Id { get; set; }
        public DateTime CreatedDate { get; set; }
        public Guid CourseId { get; set; }
        public Guid UserId { get; set; }    
        public string Type { get; set; }
        public string OS { get; set; }
    }
}
