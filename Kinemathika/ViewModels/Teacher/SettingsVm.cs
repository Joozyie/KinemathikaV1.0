// ViewModels/Teacher/SettingsVm.cs
// WHAT IT DOES: Simple settings + change password VMs (no DB yet)
using System.ComponentModel.DataAnnotations;

namespace Kinemathika.ViewModels.Teacher
{
    public class SettingsVm
    {
        [Required, Display(Name = "First Name")] public string FirstName { get; set; } = "";
        [Required, Display(Name = "Last Name")] public string LastName { get; set; } = "";
        [Required, EmailAddress, Display(Name = "Email Address")] public string Email { get; set; } = "";
    }

    public class ChangePasswordVm
    {
        [Required, DataType(DataType.Password)] public string CurrentPassword { get; set; } = "";
        [Required, DataType(DataType.Password)] public string NewPassword { get; set; } = "";
        [Required, DataType(DataType.Password), Display(Name = "Confirm Password")]
        public string ConfirmPassword { get; set; } = "";
    }
}
