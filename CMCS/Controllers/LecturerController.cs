using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CMCS.Models;
using CMCS.Repositories;
using CMCS.Data;
using System.Security.Claims;

namespace CMCS.Controllers
{
    public class LecturerController : Controller
    {
        private readonly IClaimRepository _claimRepository;
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<LecturerController> _logger;

        public LecturerController(IClaimRepository claimRepository, ApplicationDbContext context, IWebHostEnvironment environment, ILogger<LecturerController> logger)
        {
            _claimRepository = claimRepository;
            _context = context;
            _environment = environment;
            _logger = logger;
        }

        // GET: Lecturer Dashboard - shows list of MonthlyClaim
        public async Task<IActionResult> Dashboard()
        {
            try
            {
                // Demo: use lecturer id 1. Replace with auth-derived id if you add login.
                int lecturerId = 1;

                var claims = (await _claimRepository.GetClaimsByLecturerAsync(lecturerId)).ToList();

                // Guard: ensure a non-null list is passed
                if (claims == null) claims = new List<MonthlyClaim>();

                // Provide some view data for header widgets
                ViewBag.PendingClaims = claims.Count(c => c.Status == ClaimStatus.Submitted || c.Status == ClaimStatus.UnderReview);
                ViewBag.TotalClaims = claims.Count();
                ViewBag.Lecturer = await _context.Lecturers.FindAsync(lecturerId);

                return View(claims); // View expects List<MonthlyClaim>
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading lecturer dashboard");
                TempData["ErrorMessage"] = "An error occurred loading the dashboard.";
                return RedirectToAction("Index", "Home");
            }
        }

        // GET: Create new claim form
        public async Task<IActionResult> CreateClaim()
        {
            var lecturer = await _context.Lecturers.FindAsync(1); // demo
            ViewBag.HourlyRate = lecturer?.HourlyRate ?? 250.00m;
            var model = new MonthlyClaim();
            return View(model);
        }

        // POST: Create new claim
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateClaim(MonthlyClaim claim, List<IFormFile> files)
        {
            try
            {
                claim.LecturerId = 1; // demo
                var lecturer = await _context.Lecturers.FindAsync(claim.LecturerId);
                claim.TotalAmount = claim.TotalHours * (lecturer?.HourlyRate ?? 250.00m);
                ModelState.Remove(nameof(MonthlyClaim.TotalAmount));

                if (ModelState.IsValid)
                {
                    var createdClaim = await _claimRepository.CreateClaimAsync(claim);

                    if (files != null && files.Any(f => f.Length > 0))
                    {
                        // keep existing file handling (encrypted or plain depending on your controller)
                        await HandleFileUploads(createdClaim.ClaimId, files);
                    }

                    TempData["SuccessMessage"] = "Claim submitted successfully! It is now pending approval.";
                    return RedirectToAction("Dashboard");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating claim");
                ModelState.AddModelError("", $"An error occurred while submitting the claim: {ex.Message}");
            }

            var lecturerForView = await _context.Lecturers.FindAsync(1);
            ViewBag.HourlyRate = lecturerForView?.HourlyRate ?? 250.00m;
            return View(claim);
        }

        // Other actions (ClaimDetails, EditClaim, DeleteClaim, DownloadDocument, DeleteDocument, etc.)
        // You should keep your existing implementations here. For the dashboard error fix they are not required.
        // But ensure each action returns view models that match the Razor view types.

        // Example placeholder for HandleFileUploads - keep your real implementation
        private async Task HandleFileUploads(int claimId, List<IFormFile> files)
        {
            // stub - your real logic encrypts & saves files and adds SupportingDocument records
            await Task.CompletedTask;
        }
    }
}
