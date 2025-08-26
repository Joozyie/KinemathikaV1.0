public class RStudentOverviewVm
{
    public string StudentId { get; set; } = "";
    public string StudentName { get; set; } = "";
    public string Email { get; set; } = "";

    // Concepts with progress, avg attempts, avg time, trends
    public List<RStudentConceptVm> Concepts { get; set; } = new();

    // Recent attempts table
    public List<RStudentRecentAttempt> RecentAttempts { get; set; } = new();
}

public class RStudentConceptVm
{
    public string ConceptId { get; set; } = "";
    public string ConceptName { get; set; } = "";
    public double OverallProgress { get; set; } = 0.0;   // 0–1
    public double AvgAttempts { get; set; } = 0.0;
    public double AvgTime { get; set; } = 0.0;

    public List<TrendPointVm> AttemptsTrend { get; set; } = new();
    public List<TrendPointVm> TimeTrend { get; set; } = new();
}

public class RStudentRecentAttempt
{
    public string ConceptId { get; set; } = "";
    public int ProblemNo { get; set; }
    public int AttemptsToCorrect { get; set; }
    public bool FirstTry { get; set; }
    public string Status { get; set; } = "";
    public double TimeToCorrectSec { get; set; }
    public DateTime EndedAt { get; set; }
}