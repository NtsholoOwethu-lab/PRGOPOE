using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PROGCMCS.Data;
using PROGCMCS.Models;

namespace PROGCMCS.Controllers
{
    [Authorize(Roles = "Lecturer")]
    public class LecturerController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public LecturerController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // === LECTURER DASHBOARD - View Claims ===
        public async Task<IActionResult> Dashboard()
        {
            var lecturer = await GetCurrentLecturerAsync();
            if (lecturer == null)
            {
                TempData["ErrorMessage"] = "Lecturer profile not found.";
                return RedirectToAction("Index", "Home");
            }

            var claims = await _context.MonthlyClaims
                .Where(c => c.LecturerId == lecturer.LecturerId)
                .OrderByDescending(c => c.Year)
                .ThenByDescending(c => c.Month)
                .Select(c => new LecturerClaimsListViewModel
                {
                    ClaimId = c.ClaimId,
                    Month = c.Month,
                    Year = c.Year,
                    TotalHours = c.TotalHours,
                    TotalAmount = c.TotalAmount,
                    Status = c.Status,
                    SubmissionDate = c.SubmissionDate,
                    Notes = c.Notes
                })
                .ToListAsync();

            ViewBag.LecturerName = $"{lecturer.FirstName} {lecturer.LastName}";
            return View(claims);
        }

        // === GET: Create Claim ===
        [HttpGet]
        public async Task<IActionResult> CreateClaim()
        {
            var lecturer = await GetCurrentLecturerAsync();
            if (lecturer == null)
            {
                TempData["ErrorMessage"] = "Lecturer profile not found.";
                return RedirectToAction("Dashboard");
            }

            var model = new LecturerClaimViewModel
            {
                HourlyRate = lecturer.HourlyRate,
                MaxMonthlyHours = lecturer.MaxMonthlyHours
            };

            // Pass lecturer information to the view
            ViewBag.LecturerName = $"{lecturer.FirstName} {lecturer.LastName}";
            ViewBag.LecturerDepartment = lecturer.Department;

            return View(model);
        }

        // === POST: Create Claim ===
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateClaim(LecturerClaimViewModel model)
        {
            var lecturer = await GetCurrentLecturerAsync();
            if (lecturer == null)
            {
                TempData["ErrorMessage"] = "Lecturer profile not found.";
                return RedirectToAction("Dashboard");
            }

            // Use the lecturer's actual MaxMonthlyHours
            var maxHours = lecturer.MaxMonthlyHours;

            // Validate monthly hours limit
            if (model.TotalHours > maxHours)
            {
                ModelState.AddModelError(nameof(model.TotalHours),
                    $"Hours cannot exceed maximum monthly limit of {maxHours} hours.");
            }

            // Check for duplicate claim for same month/year
            var existingClaim = await _context.MonthlyClaims
                .FirstOrDefaultAsync(c => c.LecturerId == lecturer.LecturerId &&
                                         c.Month == model.Month &&
                                         c.Year == model.Year);

            if (existingClaim != null)
            {
                ModelState.AddModelError(string.Empty,
                    $"A claim for {model.Month}/{model.Year} already exists.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var claim = new MonthlyClaim
                    {
                        LecturerId = lecturer.LecturerId,
                        Month = model.Month,
                        Year = model.Year,
                        TotalHours = model.TotalHours,
                        HourlyRate = lecturer.HourlyRate,
                        TotalAmount = lecturer.HourlyRate * model.TotalHours,
                        Status = ClaimStatus.Submitted,
                        SubmissionDate = DateTime.Now,
                        Notes = model.Notes
                    };

                    _context.MonthlyClaims.Add(claim);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = $"Claim for {model.Month}/{model.Year} submitted successfully! Total amount: {claim.TotalAmount:C}";
                    return RedirectToAction(nameof(Dashboard));
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = $"Error submitting claim: {ex.Message}";
                }
            }

            // Repopulate lecturer data if validation fails
            model.HourlyRate = lecturer.HourlyRate;
            model.MaxMonthlyHours = lecturer.MaxMonthlyHours;

            // Pass lecturer information to the view again
            ViewBag.LecturerName = $"{lecturer.FirstName} {lecturer.LastName}";
            ViewBag.LecturerDepartment = lecturer.Department;

            return View(model);
        }

        // === Claim Details ===
        // === Claim Details ===
        public async Task<IActionResult> ClaimDetails(int id)
        {
            var lecturer = await GetCurrentLecturerAsync();
            if (lecturer == null)
            {
                TempData["ErrorMessage"] = "Lecturer profile not found.";
                return RedirectToAction("Dashboard");
            }

            var claim = await _context.MonthlyClaims
                .Include(c => c.Lecturer)
                .Include(c => c.SupportingDocuments)
                .Include(c => c.ClaimApprovals) // Make sure this is included
                .FirstOrDefaultAsync(c => c.ClaimId == id && c.LecturerId == lecturer.LecturerId);

            if (claim == null)
            {
                TempData["ErrorMessage"] = "Claim not found.";
                return RedirectToAction("Dashboard");
            }

            return View(claim);
        }

        // === Helper Methods ===
        private async Task<Lecturer?> GetCurrentLecturerAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return null;

            return await _context.Lecturers
                .FirstOrDefaultAsync(l => l.Email == user.Email);
        }
    }
}