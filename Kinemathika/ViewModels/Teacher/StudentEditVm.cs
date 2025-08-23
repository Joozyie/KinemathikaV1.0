// WHAT IT DOES: ViewModel for Create/Edit Student forms.
namespace Kinemathika.ViewModels.Teacher
{
    public class StudentEditVm
    {
        public Guid? Id { get; set; }
        public Guid? ClassId { get; set; }
        public string? Name { get; set; }
        public string? Email { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
