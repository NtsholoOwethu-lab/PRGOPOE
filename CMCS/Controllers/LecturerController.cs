using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CMCS.Models;
using CMCS.Repositories;
using CMCS.Data;
using CMCS.Services;

namespace CMCS.Controllers
{
    public class LecturerController : Controller
    {
        private readonly IClaimRepository _claimRepository;
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<LecturerController> _logger;

        private const int MaxFileSize = 10 * 1024 * 1024; // 10MB
        private static readonly string[] AllowedExtensions = { ".pdf", ".docx", ".xlsx", ".jpg", ".jpeg", ".png" };

        public LecturerController(IClaimRepository claimRepository, ApplicationDbContext context, IWebHostEnvironment environment, ILogger<LecturerController> logger)
        {
            _claimRepository = claimRepository;
            _context = context;
            _environment = environment;
            _logger = logger;
        }

        // GET: View all claims for lecturer
        public async Task<IActionResult> Dashboard()
        {
            // For demo purposes, using lecturer ID 1
            int lecturerId = 1;
            var claims = await _claimRepository.GetClaimsByLecturerAsync(lecturerId);

            ViewBag.PendingClaims = claims.Count(c => c.Status == ClaimStatus.Submitted || c.Status == ClaimStatus.UnderReview);
            ViewBag.TotalClaims = claims.Count();
            ViewBag.Lecturer = await _context.Lecturers.FindAsync(lecturerId);

            return View(claims);
        }

        // GET: Create new claim form
        public async Task<IActionResult> CreateClaim()
        {
            // For demo purposes, using lecturer ID 1
            var lecturer = await _context.Lecturers.FindAsync(1);
            ViewBag.HourlyRate = lecturer?.HourlyRate ?? 250.00m;

            // Provide an empty MonthlyClaim to the view
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
                // For demo purposes, using lecturer ID 1
                claim.LecturerId = 1;

                var lecturer = await _context.Lecturers.FindAsync(claim.LecturerId);
                // Compute the total server-side (authoritative)
                claim.TotalAmount = claim.TotalHours * (lecturer?.HourlyRate ?? 250.00m);

                // Remove TotalAmount from ModelState so ModelState.IsValid is evaluated against server-calculated value
                ModelState.Remove(nameof(MonthlyClaim.TotalAmount));

                if (ModelState.IsValid)
                {
                    var createdClaim = await _claimRepository.CreateClaimAsync(claim);

                    // Handle file uploads (encrypted)
                    if (files != null && files.Any(f => f.Length > 0))
                    {
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

            // If we get here something failed - re-populate the hourly rate and return the view with the posted model
            var lecturerForView = await _context.Lecturers.FindAsync(1);
            ViewBag.HourlyRate = lecturerForView?.HourlyRate ?? 250.00m;
            return View(claim);
        }

        // GET: View claim details
        public async Task<IActionResult> ClaimDetails(int id)
        {
            var claim = await _claimRepository.GetClaimByIdAsync(id);
            if (claim == null)
            {
                TempData["ErrorMessage"] = "Claim not found.";
                return RedirectToAction("Dashboard");
            }

            // Check if the claim belongs to the current lecturer (for demo, using lecturer ID 1)
            if (claim.LecturerId != 1)
            {
                TempData["ErrorMessage"] = "You don't have permission to view this claim.";
                return RedirectToAction("Dashboard");
            }

            return View(claim);
        }

        // GET: Edit claim (only if status is Draft)
        public async Task<IActionResult> EditClaim(int id)
        {
            var claim = await _claimRepository.GetClaimByIdAsync(id);
            if (claim == null)
            {
                TempData["ErrorMessage"] = "Claim not found.";
                return RedirectToAction("Dashboard");
            }

            // Only allow editing if claim is in Draft status
            if (claim.Status != ClaimStatus.Draft)
            {
                TempData["ErrorMessage"] = "You can only edit claims that are in Draft status.";
                return RedirectToAction("Dashboard");
            }

            if (claim.LecturerId != 1)
            {
                TempData["ErrorMessage"] = "You don't have permission to edit this claim.";
                return RedirectToAction("Dashboard");
            }

            var lecturer = await _context.Lecturers.FindAsync(1);
            ViewBag.HourlyRate = lecturer?.HourlyRate ?? 250.00m;
            return View(claim);
        }

        // POST: Update claim
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditClaim(int id, MonthlyClaim claim, List<IFormFile> files)
        {
            if (id != claim.ClaimId)
            {
                TempData["ErrorMessage"] = "Claim ID mismatch.";
                return RedirectToAction("Dashboard");
            }

            try
            {
                var existingClaim = await _claimRepository.GetClaimByIdAsync(id);
                if (existingClaim == null)
                {
                    TempData["ErrorMessage"] = "Claim not found.";
                    return RedirectToAction("Dashboard");
                }

                // Only allow editing if claim is in Draft status
                if (existingClaim.Status != ClaimStatus.Draft)
                {
                    TempData["ErrorMessage"] = "You can only edit claims that are in Draft status.";
                    return RedirectToAction("Dashboard");
                }

                // Update claim properties
                existingClaim.Month = claim.Month;
                existingClaim.Year = claim.Year;
                existingClaim.TotalHours = claim.TotalHours;
                existingClaim.Notes = claim.Notes;

                var lecturer = await _context.Lecturers.FindAsync(1);
                existingClaim.TotalAmount = claim.TotalHours * (lecturer?.HourlyRate ?? 250.00m);

                // Remove TotalAmount from ModelState and validate rest
                ModelState.Remove(nameof(MonthlyClaim.TotalAmount));

                if (ModelState.IsValid)
                {
                    await _claimRepository.UpdateClaimAsync(existingClaim);

                    // Handle new file uploads
                    if (files != null && files.Any(f => f.Length > 0))
                    {
                        await HandleFileUploads(id, files);
                    }

                    TempData["SuccessMessage"] = "Claim updated successfully!";
                    return RedirectToAction("Dashboard");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating claim");
                ModelState.AddModelError("", $"An error occurred while updating the claim: {ex.Message}");
            }

            var lecturerForView = await _context.Lecturers.FindAsync(1);
            ViewBag.HourlyRate = lecturerForView?.HourlyRate ?? 250.00m;
            return View(claim);
        }

        // POST: Delete claim
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteClaim(int id)
        {
            try
            {
                var claim = await _claimRepository.GetClaimByIdAsync(id);
                if (claim == null)
                {
                    TempData["ErrorMessage"] = "Claim not found.";
                    return RedirectToAction("Dashboard");
                }

                // Only allow deletion if claim is in Draft status
                if (claim.Status != ClaimStatus.Draft)
                {
                    TempData["ErrorMessage"] = "You can only delete claims that are in Draft status.";
                    return RedirectToAction("Dashboard");
                }

                if (claim.LecturerId != 1)
                {
                    TempData["ErrorMessage"] = "You don't have permission to delete this claim.";
                    return RedirectToAction("Dashboard");
                }

                var success = await _claimRepository.DeleteClaimAsync(id);
                if (success)
                {
                    TempData["SuccessMessage"] = "Claim deleted successfully!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to delete claim. Please try again.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting claim");
                TempData["ErrorMessage"] = $"An error occurred while deleting the claim: {ex.Message}";
            }

            return RedirectToAction("Dashboard");
        }

        // POST: Delete document (unchanged server behavior)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteDocument(int documentId)
        {
            try
            {
                var document = await _claimRepository.GetDocumentByIdAsync(documentId);
                if (document == null)
                {
                    return Json(new { success = false, message = "Document not found." });
                }

                var claim = await _claimRepository.GetClaimByIdAsync(document.ClaimId);
                if (claim == null || claim.LecturerId != 1)
                {
                    return Json(new { success = false, message = "You don't have permission to delete this document." });
                }

                // Only allow deletion if claim is in Draft status
                if (claim.Status != ClaimStatus.Draft)
                {
                    return Json(new { success = false, message = "You can only delete documents from claims that are in Draft status." });
                }

                // Delete physical encrypted file
                var filePath = Path.Combine(_environment.WebRootPath, "uploads", document.FilePath);
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }

                // Delete database record
                _context.SupportingDocuments.Remove(document);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Document deleted successfully!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting document");
                return Json(new { success = false, message = $"Error deleting document: {ex.Message}" });
            }
        }

        // File download (decrypt on the fly)
        public async Task<IActionResult> DownloadDocument(int id)
        {
            var document = await _claimRepository.GetDocumentByIdAsync(id);
            if (document == null)
            {
                TempData["ErrorMessage"] = "Document not found.";
                return RedirectToAction("Dashboard");
            }

            var claim = await _claimRepository.GetClaimByIdAsync(document.ClaimId);
            if (claim == null || claim.LecturerId != 1)
            {
                TempData["ErrorMessage"] = "You don't have permission to download this document.";
                return RedirectToAction("Dashboard");
            }

            var encryptedPath = Path.Combine(_environment.WebRootPath, "uploads", document.FilePath);
            if (!System.IO.File.Exists(encryptedPath))
            {
                TempData["ErrorMessage"] = "File not found on server.";
                return RedirectToAction("Dashboard");
            }

            try
            {
                var memory = await EncryptionService.DecryptFromFileAsync(encryptedPath);
                // Return file with correct mime and original filename
                return File(memory.ToArray(), GetContentType(document.FileType), document.FileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error decrypting file");
                TempData["ErrorMessage"] = "An error occurred while preparing the file for download.";
                return RedirectToAction("Dashboard");
            }
        }

        private async Task HandleFileUploads(int claimId, List<IFormFile> files)
        {
            var uploadsPath = Path.Combine(_environment.WebRootPath, "uploads");
            if (!Directory.Exists(uploadsPath))
            {
                Directory.CreateDirectory(uploadsPath);
            }

            var documents = new List<SupportingDocument>();

            foreach (var file in files.Where(f => f.Length > 0))
            {
                // Validate file size
                if (file.Length > MaxFileSize)
                {
                    throw new Exception($"File {file.FileName} exceeds the maximum size limit of 10MB.");
                }

                // Validate file extension
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!AllowedExtensions.Contains(extension))
                {
                    throw new Exception($"File {file.FileName} has an unsupported format. Allowed formats: PDF, DOCX, XLSX, JPG, JPEG, PNG.");
                }

                // Generate unique filename (store encrypted file using GUID)
                var fileName = $"{Guid.NewGuid()}{extension}";
                var filePath = Path.Combine(uploadsPath, fileName);

                // Encrypt input stream and write to disk
                using (var inputStream = file.OpenReadStream())
                {
                    await EncryptionService.EncryptToFileAsync(inputStream, filePath);
                }

                documents.Add(new SupportingDocument
                {
                    FileName = file.FileName,
                    FileType = file.ContentType,
                    FileSize = file.Length,
                    FilePath = fileName,
                    UploadDate = DateTime.Now
                });
            }

            if (documents.Any())
            {
                await _claimRepository.AddSupportingDocumentsAsync(claimId, documents);
            }
        }

        private string GetContentType(string fileType)
        {
            return fileType switch
            {
                "application/pdf" => "application/pdf",
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "image/jpeg" => "image/jpeg",
                "image/png" => "image/png",
                _ => "application/octet-stream"
            };
        }
    }
}
