using Data.MedihubSCAppEntities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Medihub4rumEntities
{
    public class MediHubScQuizResult
    {
        public long Id { get; set; }
        public string KeyAppActive { get; set; }
        public string Result { get; set;}
        public Guid TopicId { get; set; }
        public DateTime DateAdded { get; set; }
        public string Program {  get; set; }
        public bool IsPostTest { get; set; }
        public string DataResults { get; set; }
        [ForeignKey("TopicId")]
        public Topic Topic { get; set; }
    }
}
