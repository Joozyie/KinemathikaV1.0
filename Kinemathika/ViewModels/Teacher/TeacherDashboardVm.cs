// ViewModels/Teacher/TeacherDashboardVm.cs
// WHAT IT DOES: Sidebar VM, Dashboard VM, and All Classes VM.

namespace Kinemathika.ViewModels.Teacher
{
    public class SidebarVm
    {
        public string TeacherName { get; set; } = "";
        public List<ClassCardVm> RecentClasses { get; set; } = new();
    }

    public class TeacherDashboardVm
    {
        public string TeacherName { get; set; } = "";
        public List<ClassCardVm> Classes { get; set; } = new();
        public List<StudentRowVm> Students { get; set; } = new();
        public ReportVm Report { get; set; } = new();
        public SidebarVm Sidebar { get; set; } = new();
    }

    public class TeacherClassesVm
    {
        public string TeacherName { get; set; } = "";
        public bool Archived { get; set; }
        public List<ClassCardVm> Classes { get; set; } = new();
    }

    public class ClassCardVm
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public int StudentCount { get; set; }
        public decimal AverageAccuracy { get; set; } // 0-1
        public string AvgTimeSpent { get; set; } = "";
    }

    public class ReportVm
    {
        public List<int> Bars { get; set; } = new();
        public List<string> Labels { get; set; } = new();
        public string MostCompletedWorksheet { get; set; } = "";
        public string LastAssessment { get; set; } = "";
        public List<BelowThresholdVm> BelowThreshold { get; set; } = new();
    }

    public class BelowThresholdVm
    {
        public string Name { get; set; } = "";
        public string Score { get; set; } = "";
        public string Date { get; set; } = "";
    }
}
