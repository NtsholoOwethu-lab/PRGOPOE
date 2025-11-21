using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace PROGCMCS.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;

        public AccountController(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string email, string password, bool rememberMe)
        {
            var user = await _userManager.FindByEmailAsync(email);

            if (user == null)
            {
                ModelState.AddModelError("", "Invalid login attempt");
                return View();
            }

            var result = await _signInManager.PasswordSignInAsync(user, password, rememberMe, false);

            if (!result.Succeeded)
            {
                ModelState.AddModelError("", "Invalid login attempt");
                return View();
            }

            // Get user role
            var roles = await _userManager.GetRolesAsync(user);

            // Role-based redirects
            if (roles.Contains("HR"))
                return RedirectToAction("Index", "Home");

            if (roles.Contains("Lecturer"))
                return RedirectToAction("Index", "LecturerClaims");

            if (roles.Contains("Coordinator"))
                return RedirectToAction("Index", "Coordinator");

            if (roles.Contains("Manager"))
                return RedirectToAction("Index", "Manager");

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
            return RedirectToAction("Index", "Home");
        }
    }
}
