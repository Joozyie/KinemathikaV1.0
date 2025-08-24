// Models/Student.cs
// WHAT IT DOES: Student entity with natural StudentId string you already display.
using Kinemathika.Models.Analytics;

namespace Kinemathika.Models
{
    public class Student
    {
        public string StudentId { get; set; } = string.Empty; // NVARCHAR(20)
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        // Navs
        public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
        public ICollection<AttemptRecord> Attempts { get; set; } = new List<AttemptRecord>();
    }
}
