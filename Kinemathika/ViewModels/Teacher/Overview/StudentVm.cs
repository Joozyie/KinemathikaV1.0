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
        public decimal AvgAccuracy { get; set; }       
        public decimal FirstTryRate { get; set; }      
        public decimal MasteryRate { get; set; }       

        // Per-concept breakdown
        public List<ConceptProgressVm> Concepts { get; set; } = new();

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
