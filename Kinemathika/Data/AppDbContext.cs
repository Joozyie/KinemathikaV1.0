// Data/AppDbContext.cs
// WHAT IT DOES: EF Core context & configuration for relationships and indexes.
using Kinemathika.Models;
using Kinemathika.Models.Analytics;
using Microsoft.EntityFrameworkCore;

namespace Kinemathika.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Student> Students => Set<Student>();
        public DbSet<Classroom> Classrooms => Set<Classroom>();
        public DbSet<Enrollment> Enrollments => Set<Enrollment>();
        public DbSet<Worksheet> Worksheets => Set<Worksheet>();
        public DbSet<Problem> Problems => Set<Problem>();
        public DbSet<AttemptRecord> AttemptRecords => Set<AttemptRecord>();

        protected override void OnModelCreating(ModelBuilder b)
        {
            base.OnModelCreating(b);

            // Enrollment PK
            b.Entity<Enrollment>()
                .HasKey(e => new { e.ClassroomId, e.StudentDbId });

            b.Entity<Enrollment>()
                .HasOne(e => e.Classroom)
                .WithMany(c => c.Enrollments)
                .HasForeignKey(e => e.ClassroomId)
                .OnDelete(DeleteBehavior.Cascade);

            b.Entity<Enrollment>()
                .HasOne(e => e.Student)
                .WithMany(s => s.Enrollments)
                .HasForeignKey(e => e.StudentDbId)
                .OnDelete(DeleteBehavior.Cascade);

            // Student
            b.Entity<Student>()
                .HasIndex(s => s.StudentId).IsUnique();

            // Worksheet + Problem
            b.Entity<Worksheet>()
                .HasIndex(w => new { w.ConceptId, w.WorksheetId }).IsUnique();

            b.Entity<Problem>()
                .HasIndex(p => p.ProblemId).IsUnique();

            // AttemptRecord indexes for fast teacher overview
            b.Entity<AttemptRecord>()
                .HasIndex(a => a.concept_id);
            b.Entity<AttemptRecord>()
                .HasIndex(a => a.ended_at);
            b.Entity<AttemptRecord>()
                .Property(a => a.ended_status)
                .HasConversion<string>(); // store enum as string
        }
    }
}
