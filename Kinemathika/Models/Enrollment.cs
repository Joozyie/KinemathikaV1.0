// Models/Enrollment.cs
// WHAT IT DOES: Join table Student↔Classroom.
namespace Kinemathika.Models
{
    public class Enrollment
    {
        public int ClassroomId { get; set; }
        public string StudentId { get; set; } = string.Empty;

        // Navs
        public Classroom Classroom { get; set; } = default!;
        public Student Student { get; set; } = default!;
    }
}
