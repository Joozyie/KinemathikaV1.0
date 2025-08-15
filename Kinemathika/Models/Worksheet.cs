// Models/Worksheet.cs
// WHAT IT DOES: Concept worksheet (e.g., Distance, Velocity).
namespace Kinemathika.Models
{
    public class Worksheet
    {
        public int Id { get; set; }
        public string ConceptId { get; set; } = default!;   // e.g., "Distance"
        public string WorksheetId { get; set; } = default!; // external key like W-01

        public ICollection<Problem> Problems { get; set; } = new List<Problem>();
    }
}
