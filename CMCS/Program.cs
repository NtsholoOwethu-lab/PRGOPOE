using CMCS.Data;
using CMCS.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllersWithViews();

// EF In-Memory DB for prototype
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseInMemoryDatabase("CMCS-InMemory"));

// Register repository
builder.Services.AddScoped<IClaimRepository, ClaimRepository>();

// Add HttpContextAccessor for Layout
builder.Services.AddHttpContextAccessor();

// Antiforgery
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
});

// Cookie authentication (simple role-picker demo)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    });

var app = builder.Build();

// Seed data
using (var scope = app.Services.CreateScope())
{
    var ctx = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    ctx.Database.EnsureCreated();
    if (!ctx.Lecturers.Any())
    {
        ctx.Lecturers.AddRange(
            new CMCS.Models.Lecturer { LecturerId = 1, FirstName = "John", LastName = "Smith", Email = "john.smith@university.com", HourlyRate = 250.00m, Department = "Computer Science" },
            new CMCS.Models.Lecturer { LecturerId = 2, FirstName = "Sarah", LastName = "Johnson", Email = "sarah.johnson@university.com", HourlyRate = 275.00m, Department = "IT" }
        );

        ctx.MonthlyClaims.AddRange(
            new CMCS.Models.MonthlyClaim { ClaimId = 1, LecturerId = 1, Month = 10, Year = 2024, TotalHours = 40, TotalAmount = 10000, Status = CMCS.Models.ClaimStatus.Submitted, SubmissionDate = DateTime.Now.AddDays(-5) }
        );

        ctx.SaveChanges();
    }
}

// Configure middleware
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

// Ensure uploads folder exists
var uploadsPath = Path.Combine(app.Environment.WebRootPath ?? "wwwroot", "uploads");
if (!Directory.Exists(uploadsPath))
    Directory.CreateDirectory(uploadsPath);

// Serve uploads (optional)
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsPath),
    RequestPath = "/uploads"
});

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
