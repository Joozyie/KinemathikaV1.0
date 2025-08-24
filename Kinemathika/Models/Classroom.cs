// Models/Classroom.cs
// WHAT IT DOES: A teacher’s class (renamed Classroom to avoid C# keyword).
using Kinemathika.Models.Analytics;

namespace Kinemathika.Models
{

    public class Classroom
    {
        public int ClassroomId { get; set; }
        public string ClassName { get; set; } = string.Empty;
        public bool IsArchived { get; set; }

        // Navs
        public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
        public ICollection<AttemptRecord> Attempts { get; set; } = new List<AttemptRecord>();
    }
}
