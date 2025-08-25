// ViewModels/Teacher/Overview/TeacherOverviewVm.cs

namespace Kinemathika.ViewModels.Teacher
{
    public class TeacherOverviewVm
    {
        public string? ClassName { get; set; }
        public required RDashboardOverviewVm Dashboard { get; set; }
    }

}
