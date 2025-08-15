// Models/Problem.cs
// WHAT IT DOES: A specific problem inside a worksheet.
namespace Kinemathika.Models
{
    public class Problem
    {
        public int Id { get; set; }
        public string ProblemId { get; set; } = default!;   // e.g., "P-001"

        public int WorksheetId { get; set; }
        public Worksheet Worksheet { get; set; } = default!;
    }
}
