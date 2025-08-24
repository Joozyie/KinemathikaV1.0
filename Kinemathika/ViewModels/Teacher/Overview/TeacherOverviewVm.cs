// ViewModels/Teacher/Overview/TeacherOverviewVm.cs
namespace Kinemathika.ViewModels.Teacher
{
    public class TeacherOverviewVm
    {
        public int TotalStudents { get; set; }
        public int TotalClasses { get; set; }
        public decimal AvgAccuracy { get; set; }
        public double AvgTimeToCorrectSec { get; set; }
        public decimal FirstTryCorrectRate { get; set; }
        public decimal MasteryRate { get; set; }
        public List<string> Concepts { get; set; } = new();
        public List<int> ConceptAvgAccuracyPct { get; set; } = new();
        public List<double> ConceptAvgAttempts { get; set; } = new();
        public List<RecentAttemptRow> RecentAttempts { get; set; } = new();
    }

}
