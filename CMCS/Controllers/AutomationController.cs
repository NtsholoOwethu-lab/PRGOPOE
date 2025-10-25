using CMCS.Data;
using CMCS.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace CMCS.Controllers
{
    public class AutomationController : Controller
    {
        private readonly AutomationService _automation;
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _env;

        public AutomationController(AutomationService automation, ApplicationDbContext db, IWebHostEnvironment env)
        {
            _automation = automation;
            _db = db;
            _env = env;
        }

        // Simple dashboard / actions page (can be a modal or button)
        public IActionResult Index()
        {
            return View(); // we'll give a tiny view snippet below
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RunAutomation()
        {
            try
            {
                var result = await _automation.RunAutomationAsync();
                TempData["SuccessMessage"] = $"Automation complete. Verified: {result.VerifiedCount}, Approved: {result.ApprovedCount}.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Automation failed: " + ex.Message;
            }
            return RedirectToAction("Index");
        }

        // Download CSV of approved claims
        public async Task<IActionResult> DownloadApprovedClaimsCsv(DateTime? from = null, DateTime? to = null)
        {
            var csv = await _automation.BuildApprovedClaimsCsvAsync(from, to);
            var bytes = Encoding.UTF8.GetBytes(csv);
            var fileName = $"CMCS_ApprovedClaims_{DateTime.Now:yyyyMMdd_HHmm}.csv";
            return File(bytes, "text/csv", fileName);
        }

        // Generate a minimal PowerPoint summary (optional). Requires DocumentFormat.OpenXml.
        public async Task<IActionResult> DownloadPresentation()
        {
            // create pptx in temp path and return as FileStreamResult
            var tmp = Path.Combine(Path.GetTempPath(), $"CMCS_Summary_{Guid.NewGuid()}.pptx");

            try
            {
                PresentationBuilder.BuildSimplePresentation(_db, tmp); // where is thew variable name
                var fs = System.IO.File.OpenRead(tmp);
                return File(fs, "application/vnd.openxmlformats-officedocument.presentationml.presentation", Path.GetFileName(tmp));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Failed to build presentation: " + ex.Message;
                return RedirectToAction("Index");
            }
            finally
            {
                // The file will be deleted by OS temp cleanup; if you prefer immediate deletion, copy to memory stream and delete file.
            }
        }
    }
}
