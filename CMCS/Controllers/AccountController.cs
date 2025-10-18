using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace CMCS.Controllers
{
    public class AccountController : Controller
    {
        [HttpGet]
        public IActionResult Login()
        {
            // Simple role picker view
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string role)
        {
            // role can be "Lecturer", "ProgrammeCoordinator", "AcademicManager"
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, role),
                new Claim(ClaimTypes.Role, role)
            };

            // Map lecturer id for demo when role is Lecturer
            if (role == "Lecturer")
            {
                claims.Add(new Claim(ClaimTypes.NameIdentifier, "1")); // demo lecturer id 1
            }
            else if (role == "ProgrammeCoordinator")
            {
                claims.Add(new Claim(ClaimTypes.NameIdentifier, "2")); // demo approver id
            }
            else if (role == "AcademicManager")
            {
                claims.Add(new Claim(ClaimTypes.NameIdentifier, "3")); // demo manager id
            }

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }
    }
}
