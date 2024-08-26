using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.MedihubSCAppEntities
{
    public class PharmacomSurveyVote
    {
        [Key]
        public long Id { get; set; }
        public string? VoteContent { get; set; }
        public long PharmacomSurveyId { get; set;}
        public long PharmacomSurveyDetailId { get; set; }
        public string? Evaluation { get; set; }
        public DateTime DateCreated { get; set; }
        public string KeyCodeActive { get; set; }
        public Guid? SubmitId { get; set;}

        [ForeignKey("PharmacomSurveyId")]
        public PharmacomSurvey Lesson { get; set; }

        [ForeignKey("PharmacomSurveyDetailId")]
        public PharmacomSurveyDetail Question { get; set; }

    }
}
