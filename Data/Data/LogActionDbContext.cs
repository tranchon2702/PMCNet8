
using Data.LogActionEntities;
using Data.Medihub4rumEntities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Data.Data
{
    public class LogActionDbContext : DbContext
    {
        public LogActionDbContext(DbContextOptions<LogActionDbContext> options) : base(options)
        {
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<LogClickCourse>().HasKey(m => new { m.Id });
            modelBuilder.Entity<LogLesson>().HasKey(m => new { m.Id });
        }
        public  DbSet<LogClickCourse> LogClickCourse { get; set; }
        public DbSet<LogLesson> LogLesson { get; set; }

    }
}
