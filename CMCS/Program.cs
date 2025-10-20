using CMCS.Data;
using CMCS.Repositories;
using CMCS.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Register HttpContextAccessor so Razor layout can use it
builder.Services.AddHttpContextAccessor();

// Antiforgery (JS header name)
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
});

// Register EF Core In-Memory (prototype)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseInMemoryDatabase("CMCS-InMemory"));

// Register your repository implementation so controllers can receive IClaimRepository
builder.Services.AddScoped<IClaimRepository, ClaimRepository>();

// Optional: simple cookie auth (if you implemented AccountController login)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    });

var app = builder.Build();

// Ensure uploads directory exists
var uploadsPath = Path.Combine(app.Environment.WebRootPath ?? "wwwroot", "uploads");
if (!Directory.Exists(uploadsPath))
{
    Directory.CreateDirectory(uploadsPath);
}

// Seed in-memory DB (run AFTER app.Build())
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    // create DB
    context.Database.EnsureCreated();

    // Seed lecturers if none exist
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

// serve uploads folder explicitly (optional)
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(Path.Combine(app.Environment.WebRootPath ?? "wwwroot", "uploads")),
    RequestPath = "/uploads"
});

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
