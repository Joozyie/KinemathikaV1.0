// Models/Student.cs
// WHAT IT DOES: Student entity with natural StudentId string you already display.
namespace Kinemathika.Models
{
    public class Student
    {
        public int Id { get; set; }
        public string StudentId { get; set; } = default!;
        public string Name { get; set; } = default!;
        public string Email { get; set; } = default!;

        public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
        public ICollection<Analytics.AttemptRecord> Attempts { get; set; } = new List<Analytics.AttemptRecord>();
    }
}
