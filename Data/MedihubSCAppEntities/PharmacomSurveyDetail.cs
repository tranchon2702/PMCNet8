using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.MedihubSCAppEntities
{
    public class PharmacomSurveyDetail
    {
        [Key]
        public long Id { get; set; }
        public string QuestionTitle { get; set; }
        public string Type { get; set; }
        public string SurveyContent { get; set; }
        public string Status { get; set; }
        public DateTime DateCreated { get; set; }
        public string Pattern { get; set; }
        public long  PharmacomSurveyId { get; set; }
        public int Order {  get; set; }
        public bool IsLock { get; set; }
        public string Description { get; set; }
        public string LinkImageorVideo { get; set; }
        public int Page { get; set; }
        public bool IsRequired { get; set; }
        public bool IsQuestion { get; set; }

        [ForeignKey("PharmacomSurveyId")]
        public PharmacomSurvey Lesson { get; set; }


        
    }
}
