// Controller: Handles GET/POST for Login and Forgot Password (UI flow only)
using Microsoft.AspNetCore.Mvc;
using Kinemathika.ViewModels.Account;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;

namespace Kinemathika.Controllers
{
    public class AccountController : Controller
    {

        private SupabaseAuth _supabase;
        private SupabaseTokenManager _tokenManager;

        public AccountController(SupabaseAuth supabaseClient, SupabaseTokenManager tokenManager)
        {
            _supabase = supabaseClient;
            _tokenManager = tokenManager;
        }

        // GET /Account/Login
        // Shows the login page/card
        [HttpGet]
        public IActionResult Login()
        {
            return View(new LoginViewModel());
        }

        // POST /Account/Login
        // Validates credentials
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            // Supabase Login
            var result = await _supabase.SignInAsync(model.Email, model.Password);
            // Check #2 for api call results (just in case)
            if (result == null || string.IsNullOrEmpty(result.AccessToken))
            {
                ModelState.AddModelError("", "Invalid credentials");
                return View(model);
            }
            // If login successful, sign in with ASP.NET auth flow
            // Store supabase access and refresh tokens 
            _tokenManager.SetAuthToken(result);
            // Create custom ASP.NET auth identity
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, result.User.Id),
                new Claim(ClaimTypes.Email, result.User.Email),
                new Claim(ClaimTypes.Role, result.User.UserMetadata["role"].ToString() ?? "student"),
                new Claim("school_id", result.User.UserMetadata["school_id"]?.ToString() ?? "")
            };
            // Build Identity
            var identity = new ClaimsIdentity(claims, "Cookies");
            var principal = new ClaimsPrincipal(identity);
            // ASP.NET Login
            await HttpContext.SignInAsync("Cookies", principal);
            // Redirect
            TempData["Toast"] = "Welcome back!";
            return RedirectToAction("Dashboard", "Teacher");
        }

        // POST /Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            var accessToken = _tokenManager.GetToken("sb-access-token");
            _supabase.SetAccessToken(accessToken ?? "");
            // Supabase Logout
            await _supabase.LogoutAsync();
            _tokenManager.ClearTokens(); // Clear stored supabase tokens
            // ASP.NET Logout
            await HttpContext.SignOutAsync("Cookies");
            // Redirect
            TempData["Toast"] = "Welcome back!";
            return RedirectToAction("Login", "Account");
        }

        // FORGOT PASSWORD FLOW NOT YET IMPLEMENTED MATTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTT
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
