using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using PROGCMCS.Models;
using PROGCMCS.Services;

namespace PROGCMCS.Controllers
{
    [Authorize(Roles = "HR")]
    public class HRController : Controller
    {
        private readonly HRService _hrService;

        public HRController(HRService hrService)
        {
            _hrService = hrService;
        }

        public async Task<IActionResult> Workers()
        {
            var users = await _hrService.GetAllUsersAsync();
            return View(users);
        }

        [HttpGet]
        public IActionResult AddWorker()
        {
            var model = new CreateUserViewModel();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddWorker(CreateUserViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var (success, message, generatedPassword) = await _hrService.CreateUserAsync(model);

            if (success)
            {
                TempData["SuccessMessage"] = $"{message} Generated password: {generatedPassword}";
                return RedirectToAction(nameof(Workers));
            }
            else
            {
                TempData["ErrorMessage"] = message;
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditWorker(string userId)
        {
            var users = await _hrService.GetAllUsersAsync();
            var user = users.FirstOrDefault(u => u.UserId == userId);

            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction(nameof(Workers));
            }

            var model = new EditUserViewModel
            {
                UserId = user.UserId,
                LecturerId = user.LecturerId,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Department = user.Department,
                HourlyRate = user.HourlyRate,
                Role = user.Role,
                IsActive = user.IsActive
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditWorker(EditUserViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var (success, message) = await _hrService.UpdateUserAsync(model);

            if (success)
            {
                TempData["SuccessMessage"] = message;
            }
            else
            {
                TempData["ErrorMessage"] = message;
            }

            return RedirectToAction(nameof(Workers));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(string userId)
        {
            var (success, message) = await _hrService.ResetPasswordAsync(userId);

            if (success)
            {
                TempData["SuccessMessage"] = message;
            }
            else
            {
                TempData["ErrorMessage"] = message;
            }

            return RedirectToAction(nameof(Workers));
        }
    }
}