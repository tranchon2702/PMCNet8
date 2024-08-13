using Data.Medihub4rumEntities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.LogActionEntities
{
    public class LogLesson
    {
        public int Id { get; set; }
        public Guid TopicId { get; set; }
        public Guid UserId { get; set; }
        public DateTime DateAccess { get; set; }
        public string? Status { get; set; }
        public string? Result { get; set; }
        public bool? iStepD { get; set; }
        public bool? isPostTest { get; set; }
        public string? Point { get; set; }

    }
}
