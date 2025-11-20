using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PROGCMCS.Data;
using PROGCMCS.Models;

[Authorize(Roles = "HR")]
public class HrController : Controller
{
    private readonly ApplicationDbContext _db;

    public HrController(ApplicationDbContext db) => _db = db;

    public IActionResult Index() => RedirectToAction("Summary");

    public async Task<IActionResult> Summary()
    {
        var model = new HrDashboardViewModel
        {
            TotalClaims = await _db.MonthlyClaims.CountAsync(),
            TotalApprovedAmount = await _db.MonthlyClaims.Where(c => c.Status == ClaimStatus.Approved).SumAsync(c => c.TotalAmount),
            SubmittedClaims = await _db.MonthlyClaims.CountAsync(c => c.Status == ClaimStatus.Submitted),
            RejectedClaims = await _db.MonthlyClaims.CountAsync(c => c.Status == ClaimStatus.Rejected),
            PaidClaims = await _db.MonthlyClaims.CountAsync(c => c.Status == ClaimStatus.Paid)
        };
        return View(model);
    }

    public async Task<IActionResult> AllClaims() =>
        View(await _db.MonthlyClaims.Include(c => c.Lecturer).OrderByDescending(c => c.SubmissionDate).ToListAsync());

    public async Task<IActionResult> Employees() =>
        View(await _db.Lecturers.ToListAsync());

    public IActionResult AddEmployee() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddEmployee(Lecturer lecturer)
    {
        if (ModelState.IsValid)
        {
            _db.Lecturers.Add(lecturer);
            await _db.SaveChangesAsync();
            TempData["SuccessMessage"] = "Employee added successfully.";
            return RedirectToAction("Employees");
        }
        return View(lecturer);
    }

    public async Task<IActionResult> RemoveEmployee(int id)
    {
        var employee = await _db.Lecturers.FindAsync(id);
        if (employee != null)
        {
            _db.Lecturers.Remove(employee);
            await _db.SaveChangesAsync();
            TempData["SuccessMessage"] = "Employee removed successfully.";
        }
        return RedirectToAction("Employees");
    }

    public async Task<IActionResult> ExportReports()
    {
        var csv = new StringBuilder();
        csv.AppendLine("Lecturer,Email,Month,Year,TotalHours,HourlyRate,TotalAmount,Status");
        var claims = await _db.MonthlyClaims.Include(c => c.Lecturer).ToListAsync();
        foreach (var c in claims)
            csv.AppendLine($"{c.Lecturer.FirstName} {c.Lecturer.LastName},{c.Lecturer.Email},{c.Month},{c.Year},{c.TotalHours},{c.HourlyRate},{c.TotalAmount},{c.Status}");

        return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", $"Claims_{DateTime.Now:yyyyMMdd}.csv");
    }
}
