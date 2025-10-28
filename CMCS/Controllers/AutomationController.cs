using CMCS.Data;
using CMCS.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace CMCS.Controllers
{
    public class AutomationController : Controller
    {
        private readonly AutomationService _automation;

        public AutomationController(AutomationService automation)
        {
            _automation = automation;
        }

        public IActionResult Index() => View();

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

        [HttpGet]
        public async Task<IActionResult> DownloadApprovedClaimsCsv()
        {
            var csv = await _automation.BuildApprovedClaimsCsvAsync();
            var bytes = Encoding.UTF8.GetBytes(csv);
            return File(bytes, "text/csv", $"ApprovedClaims_{DateTime.Now:yyyyMMdd}.csv");
        }

        [HttpGet]
        public async Task<IActionResult> GenerateInvoicePdf(int id)
        {
            var pdfBytes = await _automation.BuildInvoicePdfAsync(id);
            return File(pdfBytes, "application/pdf", $"Invoice_Claim_{id}.pdf");
        }
    }
}
