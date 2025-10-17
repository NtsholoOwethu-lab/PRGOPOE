using CMCS.Data;
using CMCS.Models;
using CMCS.Repositories;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Use In-Memory Database instead of SQL Server
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseInMemoryDatabase("CMCS-InMemory"));

// Add repositories
builder.Services.AddScoped<IClaimRepository, ClaimRepository>();

// Configure file upload limits
builder.Services.Configure<IISServerOptions>(options =>
{
    options.MaxRequestBodySize = 50 * 1024 * 1024; // 50MB
});

var app = builder.Build();

// Configure the HTTP request pipeline.
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
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Ensure uploads directory exists
var uploadsPath = Path.Combine(app.Environment.WebRootPath, "uploads");
if (!Directory.Exists(uploadsPath))
{
    Directory.CreateDirectory(uploadsPath);
}

// Initialize the in-memory database with seed data
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    // This will create the in-memory database and seed it
    context.Database.EnsureCreated();

    // Ensure seed data is added
    if (!context.Lecturers.Any())
    {
        // Add seed data manually since HasData doesn't work well with in-memory
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

app.Run();