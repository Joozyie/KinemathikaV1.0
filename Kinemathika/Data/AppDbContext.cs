// WHAT IT DOES: EF Core context aligned to the rebuilt SQL + maps problem_no.
using Kinemathika.Models;
using Kinemathika.Models.Analytics;
using Microsoft.EntityFrameworkCore;

namespace Kinemathika.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Classroom> Classrooms => Set<Classroom>();
        public DbSet<Student> Students => Set<Student>();
        public DbSet<Enrollment> Enrollments => Set<Enrollment>();
        public DbSet<AttemptRecord> AttemptRecords => Set<AttemptRecord>();

        protected override void OnModelCreating(ModelBuilder b)
        {
            base.OnModelCreating(b);

            // Classrooms
            b.Entity<Classroom>(e =>
            {
                e.ToTable("Classrooms");
                e.HasKey(x => x.ClassroomId);
                e.Property(x => x.ClassroomId).HasColumnName("ClassroomId");
                e.Property(x => x.ClassName).HasColumnName("ClassName").HasMaxLength(100).IsRequired();
                e.Property(x => x.IsArchived).HasColumnName("IsArchived").HasDefaultValue(false);
            });

            // Students
            b.Entity<Student>(e =>
            {
                e.ToTable("Students");
                e.HasKey(x => x.StudentId);
                e.Property(x => x.StudentId).HasColumnName("StudentId").HasMaxLength(20);
                e.Property(x => x.Name).HasColumnName("Name").HasMaxLength(120).IsRequired();
                e.Property(x => x.Email).HasColumnName("Email").HasMaxLength(256).IsRequired();
            });

            // Enrollments (composite PK)
            b.Entity<Enrollment>(e =>
            {
                e.ToTable("Enrollments");
                e.HasKey(x => new { x.ClassroomId, x.StudentId });
                e.Property(x => x.ClassroomId).HasColumnName("ClassroomId");
                e.Property(x => x.StudentId).HasColumnName("StudentId").HasMaxLength(20);

                e.HasOne(x => x.Classroom).WithMany(c => c.Enrollments).HasForeignKey(x => x.ClassroomId)
                    .OnDelete(DeleteBehavior.Cascade);
                e.HasOne(x => x.Student).WithMany(s => s.Enrollments).HasForeignKey(x => x.StudentId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasIndex(x => x.ClassroomId).HasDatabaseName("IX_Enrollments_Classroom");
                e.HasIndex(x => x.StudentId).HasDatabaseName("IX_Enrollments_Student");
            });

            // AttemptRecords (snake_case + indexes)
            b.Entity<AttemptRecord>(e =>
            {
                e.ToTable("AttemptRecords");
                e.HasKey(x => x.AttemptId);

                e.Property(x => x.AttemptId).HasColumnName("attempt_id");
                e.Property(x => x.ClassId).HasColumnName("class_id");
                e.Property(x => x.StudentId).HasColumnName("student_id").HasMaxLength(20);
                e.Property(x => x.ConceptId).HasColumnName("concept_id").HasMaxLength(10);
                e.Property(x => x.AttemptsToCorrect).HasColumnName("attempts_to_correct");
                e.Property(x => x.TimeToCorrectMs).HasColumnName("time_to_correct_ms");
                e.Property(x => x.EndedAt).HasColumnName("ended_at");
                e.Property(x => x.ProblemNo).HasColumnName("problem_no");

                e.HasOne(x => x.Class).WithMany(c => c.Attempts).HasForeignKey(x => x.ClassId)
                    .OnDelete(DeleteBehavior.Cascade);
                e.HasOne(x => x.Student).WithMany(s => s.Attempts).HasForeignKey(x => x.StudentId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasIndex(x => new { x.ClassId, x.StudentId, x.EndedAt }).HasDatabaseName("IX_Attempts_ClassStudentDate");
                e.HasIndex(x => x.StudentId).HasDatabaseName("IX_Attempts_StudentDate");
                e.HasIndex(x => x.ConceptId).HasDatabaseName("IX_Attempts_Concept");
            });
        }
    }
}
