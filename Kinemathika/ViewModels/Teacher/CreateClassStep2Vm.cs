// WHAT IT DOES: VM for Step 2 (students list + selected IDs + search)
namespace Kinemathika.ViewModels.Teacher
{
    public class CreateClassStep2Vm
    {
        public string ClassName { get; set; } = "New Class";
        public List<StudentRowVm> Students { get; set; } = new();
        public List<int> SelectedIds { get; set; } = new(); // bound via name="SelectedIds"
        public string? Search { get; set; }
    }
}
