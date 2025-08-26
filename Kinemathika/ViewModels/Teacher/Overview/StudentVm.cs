using System;
using System.Collections.Generic;

namespace Kinemathika.ViewModels.Teacher
{
    public class StudentOverviewVm
    {
        // Basic info
        public string StudentId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        // Summary stats
        public int TotalAttempts { get; set; }
        public double AvgAttemptsToCorrect { get; set; }
        public double AvgTimeToCorrectSec { get; set; }
        public decimal AvgAccuracy { get; set; }       // optional
        public decimal FirstTryRate { get; set; }      // fraction of attempts solved on first try
        public decimal MasteryRate { get; set; }       // fraction of distinct problems ≤2 tries

        // Per-concept breakdown
        public List<string> Concepts { get; set; } = new();
        public List<int> ConceptAvgAccuracyPct { get; set; } = new();
        public List<double> ConceptAvgAttempts { get; set; } = new();

        // Recent attempts (latest at the top)
        public List<RecentAttemptRow> RecentAttempts { get; set; } = new();
    }

    public class RecentAttemptRow
    {
        public DateTime EndedAt { get; set; }
        public string StudentId { get; set; } = string.Empty;
        public string ConceptId { get; set; } = string.Empty;
        public string ProblemId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty; // Complete/Incomplete
        public bool FirstTry { get; set; }
        public int Attempts { get; set; }
        public double TimeSec { get; set; }
    }
}
