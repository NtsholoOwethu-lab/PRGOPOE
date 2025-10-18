using CMCS.Data;
using CMCS.Repositories;
using CMCS.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.Antiforgery;

var builder = WebApplication.CreateBuilder(args);


// Services
builder.Services.AddControllersWithViews();

// Register EF DbContext (In-Memory database for prototype)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseInMemoryDatabase("CMCS-InMemory"));

// Register repository for DI (this fixes the "Unable to resolve IClaimRepository" error)
builder.Services.AddScoped<IClaimRepository, ClaimRepository>();

// Register HttpContextAccessor (used by Layout for antiforgery token and user info)
builder.Services.AddHttpContextAccessor();

// Configure Antiforgery so JS can use a header (your layout uses a meta token)
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
});

// If you want to add logging, identity, or other services do it here
// builder.Services.AddLogging();
// builder.Services.AddAuthentication(...);

// ----------------------------
// Build app
// ----------------------------
var app = builder.Build();


// Seed sample data (optional)
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    context.Database.EnsureCreated();

    if (!context.Lecturers.Any())
    {
        context.Lecturers.AddRange(
            new Lecturer { LecturerId = 1, FirstName = "John", LastName = "Smith", Email = "john.smith@university.com", HourlyRate = 250.00m, Department = "Computer Science" },
            new Lecturer { LecturerId = 2, FirstName = "Sarah", LastName = "Johnson", Email = "sarah.johnson@university.com", HourlyRate = 275.00m, Department = "Information Technology" },
            new Lecturer { LecturerId = 3, FirstName = "David", LastName = "Brown", Email = "david.brown@university.com", HourlyRate = 300.00m, Department = "Software Engineering" }
        );

        context.MonthlyClaims.AddRange(
            new MonthlyClaim { ClaimId = 1, LecturerId = 1, Month = 10, Year = 2024, TotalHours = 40, TotalAmount = 10000, Status = ClaimStatus.Submitted, SubmissionDate = DateTime.Now.AddDays(-5) },
            new MonthlyClaim { ClaimId = 2, LecturerId = 2, Month = 9, Year = 2024, TotalHours = 35, TotalAmount = 9625, Status = ClaimStatus.Approved, SubmissionDate = DateTime.Now.AddDays(-35) },
            new MonthlyClaim { ClaimId = 3, LecturerId = 3, Month = 10, Year = 2024, TotalHours = 42, TotalAmount = 12600, Status = ClaimStatus.UnderReview, SubmissionDate = DateTime.Now.AddDays(-2) }
        );

        context.SaveChanges();
    }
}

// ----------------------------
// Middleware pipeline
// ----------------------------
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// Ensure uploads folder exists and optionally serve it under /uploads
var uploadsPath = Path.Combine(app.Environment.WebRootPath ?? "wwwroot", "uploads");
if (!Directory.Exists(uploadsPath))
{
    Directory.CreateDirectory(uploadsPath);
}
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsPath),
    RequestPath = "/uploads"
});

app.UseRouting();

// If you later enable authentication, ensure these are in the right order
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
