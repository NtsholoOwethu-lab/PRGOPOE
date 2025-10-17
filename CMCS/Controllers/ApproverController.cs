using Microsoft.AspNetCore.Mvc;
using CMCS.Models;
using CMCS.Repositories;

namespace CMCS.Controllers
{
    public class ApproverController : Controller
    {
        private readonly IClaimRepository _claimRepository;

        public ApproverController(IClaimRepository claimRepository)
        {
            _claimRepository = claimRepository;
        }

        public async Task<IActionResult> Dashboard()
        {
            var pendingClaims = await _claimRepository.GetPendingClaimsAsync();
            return View(pendingClaims);
        }

        public async Task<IActionResult> ReviewClaim(int id)
        {
            var claim = await _claimRepository.GetClaimByIdAsync(id);
            if (claim == null)
            {
                return NotFound();
            }
            return View(claim);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveClaim(int claimId, string? comments, ApproverType approverType)
        {
            try
            {
                // For demo purposes, using approver ID 1
                int approverId = 1;

                var success = await _claimRepository.ApproveClaimAsync(claimId, approverId, approverType, comments);
                if (success)
                {
                    TempData["SuccessMessage"] = "Claim approved successfully!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to approve claim. Please try again.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred: {ex.Message}";
            }

            return RedirectToAction("Dashboard");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectClaim(int claimId, string? comments, ApproverType approverType)
        {
            try
            {
                // For demo purposes, using approver ID 1
                int approverId = 1;

                var success = await _claimRepository.RejectClaimAsync(claimId, approverId, approverType, comments);
                if (success)
                {
                    TempData["SuccessMessage"] = "Claim rejected successfully!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to reject claim. Please try again.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred: {ex.Message}";
            }

            return RedirectToAction("Dashboard");
        }

        public async Task<IActionResult> ClaimHistory()
        {
            // For demo, get all claims
            var allClaims = await _claimRepository.GetClaimsByLecturerAsync(1); // This would need to be updated for production
            return View(allClaims);
        }
    }
}