// WHAT IT DOES: ViewModel for the per-student overview page (KPIs, concept stats, history).
namespace Kinemathika.ViewModels.Teacher
{
    public class StudentOverviewVm
    {
        // Identity
        public string StudentId { get; set; } = "";
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";

        // KPIs
        public int TotalAttempts { get; set; }
        public decimal AvgAccuracy { get; set; }           // 0–1
        public decimal FirstTryRate { get; set; }          // 0–1
        public decimal MasteryRate { get; set; }           // 0–1
        public double AvgTimeToCorrectSec { get; set; }    // seconds

        // Concept breakdown
        public List<string> Concepts { get; set; } = new();
        public List<int> ConceptAvgAccuracyPct { get; set; } = new();  // 0–100
        public List<double> ConceptAvgAttempts { get; set; } = new();

        // History (latest attempts for this student)
        public List<RecentAttemptRow> RecentAttempts { get; set; } = new();
    }

}