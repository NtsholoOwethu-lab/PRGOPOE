using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CMCS.Models;
using CMCS.Repositories;
using CMCS.Data;
using CMCS.Services;
using System.Security.Claims;

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

        private int GetCurrentLecturerId()
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(idClaim, out int id)) return id;
            return 1; // fallback demo
        }

        public async Task<IActionResult> Dashboard()
        {
            try
            {
                int lecturerId = GetCurrentLecturerId();
                var claims = (await _claimRepository.GetClaimsByLecturerAsync(lecturerId)).ToList();

                ViewBag.PendingClaims = claims.Count(c => c.Status == ClaimStatus.Submitted || c.Status == ClaimStatus.UnderReview);
                ViewBag.TotalClaims = claims.Count();
                ViewBag.Lecturer = await _context.Lecturers.FindAsync(lecturerId);

                return View(claims);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading dashboard");
                TempData["ErrorMessage"] = "An error occurred loading the dashboard.";
                return RedirectToAction("Index", "Home");
            }
        }

        public async Task<IActionResult> CreateClaim()
        {
            var lecturer = await _context.Lecturers.FindAsync(GetCurrentLecturerId());
            ViewBag.HourlyRate = lecturer?.HourlyRate ?? 250.00m;
            return View(new MonthlyClaim());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateClaim(MonthlyClaim claim, List<IFormFile> files)
        {
            try
            {
                claim.LecturerId = GetCurrentLecturerId();
                var lecturer = await _context.Lecturers.FindAsync(claim.LecturerId);
                claim.TotalAmount = claim.TotalHours * (lecturer?.HourlyRate ?? 250.00m);
                ModelState.Remove(nameof(MonthlyClaim.TotalAmount));

                if (ModelState.IsValid)
                {
                    var created = await _claimRepository.CreateClaimAsync(claim);

                    if (files != null && files.Any(f => f.Length > 0))
                        await HandleFileUploads(created.ClaimId, files);

                    TempData["SuccessMessage"] = "Claim submitted successfully!";
                    return RedirectToAction("Dashboard");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating claim");
                ModelState.AddModelError("", $"An error occurred while submitting the claim: {ex.Message}");
            }

            var lecturerForView = await _context.Lecturers.FindAsync(GetCurrentLecturerId());
            ViewBag.HourlyRate = lecturerForView?.HourlyRate ?? 250.00m;
            return View(claim);
        }

        public async Task<IActionResult> ClaimDetails(int id)
        {
            var claim = await _claimRepository.GetClaimByIdAsync(id);
            if (claim == null)
            {
                TempData["ErrorMessage"] = "Claim not found.";
                return RedirectToAction("Dashboard");
            }

            if (claim.LecturerId != GetCurrentLecturerId())
            {
                TempData["ErrorMessage"] = "You don't have permission to view this claim.";
                return RedirectToAction("Dashboard");
            }

            return View(claim);
        }

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
                if (claim == null || claim.LecturerId != GetCurrentLecturerId())
                    return Json(new { success = false, message = "You don't have permission to delete this document." });

                if (claim.Status != ClaimStatus.Draft)
                    return Json(new { success = false, message = "You can only delete documents from draft claims." });

                var filePath = Path.Combine(_environment.WebRootPath, "uploads", document.FilePath);
                if (System.IO.File.Exists(filePath))
                    System.IO.File.Delete(filePath);

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

        public async Task<IActionResult> DownloadDocument(int id)
        {
            var document = await _claimRepository.GetDocumentByIdAsync(id);
            if (document == null)
            {
                TempData["ErrorMessage"] = "Document not found.";
                return RedirectToAction("Dashboard");
            }

            var claim = await _claimRepository.GetClaimByIdAsync(document.ClaimId);
            if (claim == null || claim.LecturerId != GetCurrentLecturerId())
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
            if (!Directory.Exists(uploadsPath)) Directory.CreateDirectory(uploadsPath);

            var documents = new List<SupportingDocument>();

            foreach (var file in files.Where(f => f.Length > 0))
            {
                if (file.Length > MaxFileSize)
                    throw new Exception($"File {file.FileName} exceeds the maximum size limit of 10MB.");

                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!AllowedExtensions.Contains(extension))
                    throw new Exception($"File {file.FileName} has an unsupported format.");

                var fileName = $"{Guid.NewGuid()}{extension}";
                var filePath = Path.Combine(uploadsPath, fileName);

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
                await _claimRepository.AddSupportingDocumentsAsync(claimId, documents);
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
