using PROGCMCS.Data;
using PROGCMCS.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace PROGCMCS.Controllers
{
    public class LecturerController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<LecturerController> _logger;

        // Hourly rate constraints
        private const decimal HourlyRateMin = 50m;
        private const decimal HourlyRateMax = 500m;

        public LecturerController(
            ApplicationDbContext context,
            IWebHostEnvironment environment,
            ILogger<LecturerController> logger)
        {
            _context = context;
            _environment = environment;
            _logger = logger;
        }

        // === LECTURER DASHBOARD ===
        public IActionResult Dashboard()
        {
            var lecturers = _context.Lecturers
                .Include(l => l.MonthlyClaims)
                .ToList();

            return View(lecturers);
        }

        // === GET: Add Lecturer ===
        [HttpGet]
        public IActionResult AddLecturer()
        {
            return View();
        }

        // === POST: Add Lecturer ===
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddLecturer(Lecturer lecturer)
        {
            // Server-side validation for hourly rate bounds
            if (lecturer.HourlyRate < HourlyRateMin || lecturer.HourlyRate > HourlyRateMax)
            {
                ModelState.AddModelError(nameof(lecturer.HourlyRate), $"Hourly rate must be between {HourlyRateMin} and {HourlyRateMax}.");
            }

            if (ModelState.IsValid)
            {
                _context.Lecturers.Add(lecturer);
                _context.SaveChanges();

                TempData["SuccessMessage"] = "Lecturer added successfully!";
                return RedirectToAction(nameof(Dashboard));
            }

            TempData["ErrorMessage"] = "Hourly rate must be between 50 and 500.";
            return View(lecturer);
        }

        // === GET: Create Claim ===
        [HttpGet]
        public IActionResult CreateClaim()
        {
            try
            {
                // TEMPORARY FIX: Remove IsActive filter until migration is run
                ViewBag.Lecturers = _context.Lecturers
                    .Select(l => new { l.LecturerId, FullName = l.FirstName + " " + l.LastName })
                    .ToList();

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading lecturers for CreateClaim");
                ViewBag.Lecturers = new List<object>();
                return View();
            }
        }

        // === POST: Create Claim ===
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateClaim(MonthlyClaim claim)
        {
            if (!ModelState.IsValid)
            {
                // TEMPORARY FIX: Remove IsActive filter
                ViewBag.Lecturers = _context.Lecturers
                    .Select(l => new { l.LecturerId, FullName = l.FirstName + " " + l.LastName })
                    .ToList();
                return View(claim);
            }

            try
            {
                // Find lecturer by the selected LecturerId from the form
                var lecturer = await _context.Lecturers
                    .FirstOrDefaultAsync(l => l.LecturerId == claim.LecturerId);

                if (lecturer == null)
                {
                    TempData["ErrorMessage"] = "Selected lecturer not found.";

                    // TEMPORARY FIX: Remove IsActive filter
                    ViewBag.Lecturers = _context.Lecturers
                        .Select(l => new { l.LecturerId, FullName = l.FirstName + " " + l.LastName })
                        .ToList();
                    return View(claim);
                }

                // Set claim properties
                claim.HourlyRate = lecturer.HourlyRate;
                claim.TotalAmount = lecturer.HourlyRate * claim.TotalHours;
                claim.SubmissionDate = DateTime.Now;
                claim.Status = ClaimStatus.Submitted;

                _context.MonthlyClaims.Add(claim);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Claim submitted successfully!";
                return RedirectToAction(nameof(Dashboard));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating claim");
                TempData["ErrorMessage"] = "An error occurred while submitting the claim.";

                // TEMPORARY FIX: Remove IsActive filter
                ViewBag.Lecturers = _context.Lecturers
                    .Select(l => new { l.LecturerId, FullName = l.FirstName + " " + l.LastName })
                    .ToList();
                return View(claim);
            }
        }

        // === POST: Delete Document (AJAX) ===
        [HttpPost]
        public async Task<IActionResult> DeleteDocument(int id)
        {
            try
            {
                var document = _context.SupportingDocuments.FirstOrDefault(d => d.DocumentId == id);
                if (document == null)
                    return Json(new { success = false, message = "Document not found." });

                var filePath = Path.Combine(_environment.WebRootPath, "uploads", document.FilePath);
                if (System.IO.File.Exists(filePath))
                    System.IO.File.Delete(filePath);

                _context.SupportingDocuments.Remove(document);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Document {id} deleted successfully.");
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting document.");
                return Json(new { success = false, message = "Error deleting document." });
            }
        }

        // GET: Lecturer/Edit/5
        [HttpGet]
        public async Task<IActionResult> EditLecturer(int id)
        {
            var lecturer = await _context.Lecturers.FindAsync(id);
            if (lecturer == null) return NotFound();
            return View(lecturer);
        }

        // POST: Lecturer/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditLecturer(int id, Lecturer lecturer)
        {
            if (id != lecturer.LecturerId) return BadRequest();

            // Server-side validation for hourly rate bounds
            if (lecturer.HourlyRate < HourlyRateMin || lecturer.HourlyRate > HourlyRateMax)
            {
                ModelState.AddModelError(nameof(lecturer.HourlyRate), $"Hourly rate must be between {HourlyRateMin} and {HourlyRateMax}.");
            }

            if (!ModelState.IsValid) return View(lecturer);

            _context.Update(lecturer);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Lecturer updated.";
            return RedirectToAction(nameof(Dashboard));
        }
    }
}