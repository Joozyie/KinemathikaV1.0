// WHAT IT DOES: VM for Step 1 (class name only)
using System.ComponentModel.DataAnnotations;

namespace Kinemathika.ViewModels.Teacher
{
    public class CreateClassStep1Vm
    {
        [Required, StringLength(100, MinimumLength = 2)]
        [Display(Name = "Classroom Name")]
        public string ClassName { get; set; } = "";
    }
}
