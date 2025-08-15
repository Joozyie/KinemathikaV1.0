// Models/Classroom.cs
// WHAT IT DOES: A teacher’s class (renamed Classroom to avoid C# keyword).
namespace Kinemathika.Models
{
    public class Classroom
    {
        public int Id { get; set; }
        public string Name { get; set; } = default!;
        public bool IsArchived { get; set; }

        public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    }
}
