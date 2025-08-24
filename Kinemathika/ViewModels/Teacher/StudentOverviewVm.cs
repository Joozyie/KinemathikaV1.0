// ViewModels/Teacher/StudentOverviewVm.cs
// WHAT IT DOES: Student page VM; now uses the shared RecentAttemptRow (no nested class).
namespace Kinemathika.ViewModels.Teacher
{
    public class StudentOverviewVm
    {
        public string StudentId { get; set; } = "";
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";

        public int TotalAttempts { get; set; }
        public double AvgAttemptsToCorrect { get; set; }
        public decimal AvgAccuracy { get; set; }
        public decimal FirstTryRate { get; set; }
        public decimal MasteryRate { get; set; }                // used as "Progress" in the donut
        public double AvgTimeToCorrectSec { get; set; }

        public List<string> Concepts { get; set; } = new();
        public List<int> ConceptAvgAccuracyPct { get; set; } = new();
        public List<double> ConceptAvgAttempts { get; set; } = new();

        public List<RecentAttemptRow> RecentAttempts { get; set; } = new();
    }
}
