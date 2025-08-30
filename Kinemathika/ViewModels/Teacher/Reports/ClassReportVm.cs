namespace Kinemathika.ViewModels.Teacher
{
    public class ClassReportVm
    {
        public string ClassName { get; set; } = "";
        public List<StudentPerformance> StudentPerformances { get; set; } = new();
        public DateTime GeneratedAt { get; set; }
    }
}