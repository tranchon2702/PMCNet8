
using Data.Medihub4rumEntities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;


namespace Data.Data
{
    public class Medihub4rumDbContext : IdentityDbContext
    {
        public Medihub4rumDbContext(DbContextOptions<Medihub4rumDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
           
            modelBuilder.Entity<SponsorUser>().HasKey(m => new {m.SponsorId,m.UserId});
            modelBuilder.Entity<Category>().HasKey(m => m.Id);
            modelBuilder.Entity<MediHubScQuizResult>().HasKey(m => new { m.Id });
            modelBuilder.Entity<Topic>().HasKey(m => new { m.Id });
            modelBuilder.Entity<SponsorHubCourseReport>().HasKey(m => new { m.CategoryId , m.Month , m.Year });

        }

        public DbSet<SponsorHubCourseFinish> SponsorHubCourseFinish { get; set; }
        public DbSet<SponsorHub> SponsorHub { get; set; }
        public DbSet<SponsorHubCourse> SponsorHubCourse { get; set; }
        public DbSet<CategorySurvey> CategorySurvey { get; set; }
        public DbSet<SponsorUser> SponsorUser { get; set;}
        public DbSet<UserAccount> UserAccount { get; set;}
        public DbSet<Sponsor> Sponsor { get; set; }
        public DbSet<MediHubScQuizResult> MediHubScQuizResult { get; set; }
        public DbSet<Category> Category { get; set; }
        public DbSet<Topic> Topic { get; set; }
        public DbSet<MembershipUser> MembershipUser { get; set; }
        public DbSet<MembershipUserInfo> MembershipUserInfo { get; set; }
        public DbSet<MediHubSCQuiz> MediHubSCQuiz {  get; set; }
        public DbSet<SponsorHubCourseReport> SponsorHubCourseReport { get; set; }

        public DbSet<SponsorProduct> SponsorProduct { get; set;}
        public DbSet<SponsorCampaign > SponsorCampaign { get; set;}

        public DbSet<SponsorCampaignProductCode> SponsorCampaignProductCode { get; set; }

        public DbSet<SponsorCampaignProductScan> SponsorCampaignProductScan { get; set; }
    }

}
