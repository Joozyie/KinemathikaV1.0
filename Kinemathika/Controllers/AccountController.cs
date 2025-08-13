// Controller: Handles GET/POST for Login and Forgot Password (UI flow only)
using Microsoft.AspNetCore.Mvc;
using Kinemathika.ViewModels.Account;

namespace Kinemathika.Controllers
{
    public class AccountController : Controller
    {
        // GET /Account/Login
        // Shows the login page/card
        [HttpGet]
        public IActionResult Login()
        {
            return View(new LoginViewModel());
        }

        // POST /Account/Login
        // Validates credentials and shows invalid state on error
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(LoginViewModel model, string? returnUrl = null)
        {
            if (!ModelState.IsValid)
                return View(model);

            // TODO: Replace with IAuthService/Identity sign-in
            var emailOk = !string.IsNullOrWhiteSpace(model.Email);
            var passOk = model.Password == "Password123!"; // demo only

            if (!(emailOk && passOk))
            {
                ModelState.AddModelError(string.Empty, "Invalid email or password.");
                return View(model);
            }

            // You could set a TempData toast to simulate success
            TempData["Toast"] = "Welcome back!";
            // TODO: Auth cookie + Redirect to dashboard
            return RedirectToAction("Index", "Teacher");
        }

        // POST /Account/ForgotPassword
        // Accepts an email and shows the "Reset link sent" modal via TempData
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ForgotPassword([FromForm] string email)
        {
            // TODO: Implement email check + token generation + email sending
            if (string.IsNullOrWhiteSpace(email))
            {
                TempData["Toast"] = "Please enter a valid email.";
                return RedirectToAction(nameof(Login));
            }

            // Simulate success + trigger the success modal on next view
            TempData["ResetSent"] = true;
            return RedirectToAction(nameof(Login));
        }
    }
}
