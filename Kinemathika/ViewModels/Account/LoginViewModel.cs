// WHAT IT DOES: Makes Email required (not format-validated); keeps Password + RememberMe.

using System.ComponentModel.DataAnnotations;

namespace Kinemathika.ViewModels.Account
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Please enter your email.")]
        public string Email { get; set; } = string.Empty;

        [Required, DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        public bool RememberMe { get; set; }
    }
}
