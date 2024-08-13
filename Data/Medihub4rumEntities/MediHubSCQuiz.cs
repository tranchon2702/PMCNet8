using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Medihub4rumEntities
{
    public class MediHubSCQuiz
    {
        [Key]
        public long Id { get; set; }
        public string QuizName { get; set; }
        public string? QuizDescription { get; set;}
        public string? QuizImage { get; set;}
        public string? QuizQuestions { get; set;}
        public string Slug { get; set;}
        public string? RelatedSubject { get; set;}
        public DateTime CreateDate { get; set; }
        public bool IsSticky { get; set; }
        public string? FileExcel { get; set; }
        public string? Status { get; set; }
        public string? Nguon { get; set; }
        public DateTime LastUpdate {  get; set; }
        public int CustomerId { get; set; }
        public Guid TopicId { get; set; }
        public Guid UserId { get; set; }
        public bool IsPostTest { get; set; }
        public int Point {  get; set; }
        [ForeignKey("TopicId")]
        public Topic Topic { get; set; }
        [ForeignKey("UserId")]
        public MembershipUser User { get; set; }
    }
}
