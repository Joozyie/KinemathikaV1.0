// ViewModels/Account/LoginViewModel.cs
using System.ComponentModel.DataAnnotations;

namespace Kinemathika.ViewModels.Account
{
    public class LoginViewModel
    {
        [Required, EmailAddress(ErrorMessage = "Please enter a valid email.")]
        public string Email { get; set; } = string.Empty;

        [Required, DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        public bool RememberMe { get; set; }
    }
}
