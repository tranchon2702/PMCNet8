
using Data.MedihubSCAppEntities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Data.Data
{
    public class MedihubSCAppDbContext(DbContextOptions<MedihubSCAppDbContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }

        public DbSet<PharmacomSurveyVote> PharmacomSurveyVote { get; set; }
        public DbSet<PharmacomSurvey>  PharmacomSurvey {  get; set; }
        public DbSet<PharmacomSurveyDetail> PharmacomSurveyDetail { get; set; }
        public DbSet<CPECourseActive> CPECourseActive {  get; set; }
        public DbSet<AppSetup> AppSetup { get; set; }
        public DbSet<Config> Config { get; set; }
    }
}
