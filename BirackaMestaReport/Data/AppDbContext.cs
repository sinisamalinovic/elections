using Microsoft.EntityFrameworkCore;

namespace BirackaMestaReport.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<BmSubmission> Submissions => Set<BmSubmission>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<BmSubmission>()
                .HasIndex(s => s.BmId);
            modelBuilder.Entity<BmSubmission>()
                .HasIndex(s => s.ReceivedAt);
        }
    }
}
