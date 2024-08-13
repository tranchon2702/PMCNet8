using Data.MedihubSCAppEntities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Medihub4rumEntities
{
    public class CategorySurvey
    {
        [Key]
        public Guid Id { get; set; }
        
        public Guid CategoryId { get; set; }
        
        public long SurveyId { get; set; }
        public int Type {  get; set; }

        [ForeignKey("CategoryId")]
        public Category Category { get; set; }
    }
}
