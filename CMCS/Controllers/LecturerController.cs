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
            if (ModelState.IsValid)
            {
                _context.Lecturers.Add(lecturer);
                _context.SaveChanges();

                TempData["SuccessMessage"] = "Lecturer added successfully!";
                return RedirectToAction(nameof(Dashboard));
            }

            TempData["ErrorMessage"] = "Please fill in all required fields.";
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
            if (ModelState.IsValid)
            {
                var lecturer = _context.Lecturers.FirstOrDefault(l => l.LecturerId == claim.LecturerId);
                if (lecturer != null)
                {
                    claim.TotalAmount = claim.TotalHours * lecturer.HourlyRate;
                    claim.Status = ClaimStatus.Submitted; // ✅ Automatically mark as Submitted
                    claim.SubmissionDate = DateTime.Now;
                }

                _context.MonthlyClaims.Add(claim);
                _context.SaveChanges();

                TempData["SuccessMessage"] = "Claim submitted for verification!";

                // ✅ Redirect to Verifier dashboard after submission
                return RedirectToAction("Dashboard", "Verify");
            }

            // Re-populate dropdown on validation error
            ViewBag.Lecturers = _context.Lecturers.ToList();
            return View(claim);
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
    }
}
