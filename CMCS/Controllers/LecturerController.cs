using CMCS.Data;
using CMCS.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CMCS.Controllers
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
            ViewBag.Lecturers = _context.Lecturers.ToList();
            return View(new MonthlyClaim());
        }

        // === POST: Create Claim ===
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateClaim(MonthlyClaim claim)
        {
            ViewBag.Lecturers = _context.Lecturers.ToList();

            // Basic model validation first
            if (!ModelState.IsValid)
            {
                return View(claim);
            }

            var lecturer = _context.Lecturers.FirstOrDefault(l => l.LecturerId == claim.LecturerId);
            if (lecturer == null)
            {
                ModelState.AddModelError("LecturerId", "Selected lecturer was not found.");
                return View(claim);
            }

            // Validate lecturer hourly rate bounds before creating claim
            if (lecturer.HourlyRate < HourlyRateMin || lecturer.HourlyRate > HourlyRateMax)
            {
                ModelState.AddModelError(string.Empty, $"The selected lecturer has an hourly rate outside the allowed range ({HourlyRateMin} - {HourlyRateMax}). Please correct the lecturer's rate before submitting a claim.");
                return View(claim);
            }

            // Set the claim's hourly rate from lecturer and calculate total
            claim.HourlyRate = lecturer.HourlyRate;
            claim.TotalAmount = claim.TotalHours * claim.HourlyRate;
            claim.Status = ClaimStatus.Submitted; // mark as Submitted
            claim.SubmissionDate = DateTime.Now;

            _context.MonthlyClaims.Add(claim);
            _context.SaveChanges();

            TempData["SuccessMessage"] = "Claim submitted for verification!";
            return RedirectToAction("Dashboard", "Verify");
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
            if (!ModelState.IsValid) return View(lecturer);

            _context.Update(lecturer);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Lecturer updated.";
            return RedirectToAction(nameof(Dashboard));
        }
    }
}
