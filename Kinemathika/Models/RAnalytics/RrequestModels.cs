public class RStudentDto
{
    public string StudentId { get; set; } = null!;  // maps from UserId
    public string Name { get; set; } = null!;       // optional, can default to UserId
    public string Email { get; set; } = "";         // optional
}

public class RAttemptDto
{
    public string StudentId { get; set; } = null!;  // maps from UserId
    public string ConceptId { get; set; } = null!;
    public string ProblemNo { get; set; } = null!;  // maps from ProblemId
    public int AttemptsToCorrect { get; set; }
    public int TimeToCorrectMs { get; set; }
    public DateTime? EndedAt { get; set; }
}