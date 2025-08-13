// WHAT IT DOES: Row item shown in Step 2 student table
using System.ComponentModel.DataAnnotations;

namespace Kinemathika.ViewModels.Teacher
{
    public class StudentRowVm
    {
        public int Id { get; set; }
        public string StudentId { get; set; } = "";
        public string Name { get; set; } = "";
        [EmailAddress] public string Email { get; set; } = "";
    }
}
