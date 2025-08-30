using Microsoft.EntityFrameworkCore;

namespace Kinemathika.Data
{
    public class SbDbContext : DbContext
    {
        public DbSet<SbClass> Classes { get; set; }
        public DbSet<SbStudent> Students { get; set; }
        public DbSet<ProblemAttempt> ProblemAttempts { get; set; }

        public SbDbContext(DbContextOptions<SbDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // organization.class
            modelBuilder.Entity<SbClass>(entity =>
            {
                entity.ToTable("class", "organization");
                entity.HasKey(e => e.ClassId);
            });

            // accounts.student
            modelBuilder.Entity<SbStudent>(entity =>
            {
                entity.ToTable("student", "accounts");
                entity.HasKey(e => e.UserId);

                entity.HasOne(e => e.Class)
                      .WithMany(c => c.Students)
                      .HasForeignKey(e => e.ClassId);
            });

            // analytics.problemattempt
            modelBuilder.Entity<ProblemAttempt>(entity =>
            {
                entity.ToTable("problemattempt", "analytics");
                entity.HasKey(e => e.SessionId);

                entity.HasOne(pa => pa.Student)
                      .WithMany(s => s.ProblemAttempts)
                      .HasForeignKey(pa => pa.UserId);
            });
        }
    }
}
