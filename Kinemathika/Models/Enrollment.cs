// Models/Enrollment.cs
// WHAT IT DOES: Join table Student↔Classroom.
namespace Kinemathika.Models
{
    public class Enrollment
    {
        public int ClassroomId { get; set; }
        public Classroom Classroom { get; set; } = default!;

        public int StudentDbId { get; set; }
        public Student Student { get; set; } = default!;
    }
}
