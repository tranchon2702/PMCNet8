using Data.Medihub4rumEntities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.MedihubSCAppEntities
{
    public class PharmacomSurvey
    {
        [Key]
        public long Id { get; set; }
        public string? Name { get; set; }
        public string? Status { get; set; }
        public DateTime DateCreated { get; set; }
        public string? SystemSource { get; set; }
        public int? Order {  get; set; }
        public bool IsLock { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Description { get; set; }
        public string?   LinkImage { get; set; }
        public string? Pattern { get; set; }
        public string? Part {  get; set; }
        public int Point  { get; set; }

        public virtual ICollection<PharmacomSurveyDetail> Questions { get; set; }
        public virtual ICollection<PharmacomSurveyVote> Answers { get; set; }
    }
}
