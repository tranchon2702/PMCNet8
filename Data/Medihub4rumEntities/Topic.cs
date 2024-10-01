using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Medihub4rumEntities
{
    public class Topic
    {
        
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public DateTime CreateDate { get; set; }
        public bool Solved { get; set; }
        public bool SolvedReminderSent { get; set; }
        public string Slug { get; set; }
        public int Views { get; set; }
        public bool IsSticky { get; set; }
        public bool IsLocked { get;set; }
        public bool? Pending { get; set; }
        public string? Status { get; set; }
        public Guid Category_Id { get; set; }
        public Guid? Post_Id { get; set; }
        public Guid? Poll_Id { get; set; }
        public Guid MembershipUser_Id { get; set;}
        public string? MetaSeo { get; set; }
        public string? LinkImage { get; set; }
        public int TypeTopic {  get; set; }
        public string? CaptionShort { get; set; }
        public DateTime? DateApprove { get; set; }
        public string? BannerLink { get; set; }
        public string? RelatedSubject { get; set; }
        public string? LinkVideoAudio { get; set;}
        public string? Channel {  get; set; }
        public bool IsInterested { get; set; }
        public string? CateSub {  get; set; }
        public string? Author { get; set; }
        public string? SectionName { get; set; }
        public int? OrderBySection { get; set; }
        public int? Duration_Video { get; set; }
        public int? SectionId { get; set;}
        public int? TopicSection_Id { get; set; }
        public int Order {  get; set; }

        [ForeignKey("Category_Id")]
        public Category Category { get; set; }

    }
}
