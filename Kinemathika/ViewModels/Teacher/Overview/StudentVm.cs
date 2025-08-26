// Student part
public class StudentOverviewVm
{
    public string StudentId { get; set; } = "";
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";

    // Overall averages
    public double AvgAttemptsToCorrect { get; set; }
    public double AvgTimeToCorrectSec { get; set; }

    // Concept-level stats
    public List<string> Concepts { get; set; } = new();
    public List<double> ConceptAvgAttempts { get; set; } = new();
    public List<int> ConceptAvgAccuracyPct { get; set; } = new();

    // Recent attempts table
    public List<RecentAttemptRow> RecentAttempts { get; set; } = new();
}

public class RecentAttemptRow
{
    public DateTime EndedAt { get; set; }
    public string StudentId { get; set; } = "";
    public string ConceptId { get; set; } = "";
    public string ProblemId { get; set; } = "";
    public string Status { get; set; } = "";
    public bool FirstTry { get; set; }
    public int Attempts { get; set; }
    public double TimeSec { get; set; }
}
