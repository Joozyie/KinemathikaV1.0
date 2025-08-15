// WHAT IT DOES: ViewModel for rows in the Students table (Dashboard).
// FILE: ViewModels/Teacher/StudentRowVm.cs
namespace Kinemathika.ViewModels.Teacher
{
    public class StudentRowVm
    {
        public string StudentId { get; set; } = "";
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";

        // New analytics fields used by Dashboard.cshtml and TeacherController.Dashboard
        public int TotalAttempts { get; set; }
        public decimal AvgAccuracy { get; set; }     // 0–1 (e.g., 0.61 = 61%)
        public DateTime? LastActive { get; set; }
    }
}
