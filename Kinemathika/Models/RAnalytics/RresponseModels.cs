using System;
using System.Collections.Generic;

public class RDashboardOverviewVm
{
    public int TotalClasses { get; set; }
    public int TotalStudents { get; set; }
    public double AvgAttemptsPerProblem { get; set; }
    public double AvgTimePerProblem { get; set; }
    public List<ConceptProgressVm> Concepts { get; set; } = new();
}

public class ConceptProgressVm
{
    public string ConceptId { get; set; } = "";
    public string ConceptName { get; set; } = "";
    public double OverallProgress { get; set; }
    public double AvgAttempts { get; set; }
    public double AvgTime { get; set; }
    public List<TrendPointVm> AttemptsTrend { get; set; } = new();
    public List<TrendPointVm> TimeTrend { get; set; } = new();
}


public class TrendPointVm
{
    public DateOnly Date { get; set; }
    public double Value { get; set; }
}

public class StudentPerformance
{
    public string StudentId { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Email { get; set; } = null!;

    // Computed metrics
    public double ProgressPct { get; set; }       // e.g., progress % for this student
    public double AvgAttempts { get; set; }       // average attempts per problem
    public double AvgTimeSec { get; set; }        // average time to complete per problem

    public DateTime? LastActivity { get; set; }   // most recent attempt datetime
}
