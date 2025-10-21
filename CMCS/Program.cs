using CMCS.Data;

using CMCS.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;

var builder = WebApplication.CreateBuilder(args);

// ------------------------------------------------------
// 1️⃣ Configure Services
// ------------------------------------------------------

// Add MVC Controllers and Views
builder.Services.AddControllersWithViews();

// Add HttpContext accessor for Razor Views and Controllers
builder.Services.AddHttpContextAccessor();

// Register Antiforgery service (for CSRF protection)
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN"; // match your JS fetch token header
});

// ✅ Register Claim Repository (Dependency Injection)


// Optional: register other services (if any)
// builder.Services.AddScoped<IEncryptionService, EncryptionService>();

// ------------------------------------------------------
// 2️⃣ Configure Database Context
// ------------------------------------------------------

// ⚠️ If using in-memory DB for testing or demo:
// ✅ Use SQLite for persistent local storage
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// OR if you have a real SQL Server connection, use this instead:
// builder.Services.AddDbContext<ApplicationDbContext>(options =>
//     options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ------------------------------------------------------
// 3️⃣ Build the App
// ------------------------------------------------------
var app = builder.Build();

// ------------------------------------------------------
// 4️⃣ Configure HTTP Request Pipeline
// ------------------------------------------------------
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication(); // optional — if using Identity/cookies
app.UseAuthorization();

// ------------------------------------------------------
// 5️⃣ Database Seeding (Optional)
// ------------------------------------------------------
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    context.Database.EnsureCreated();

    // Example: Seed a lecturer and multiple sample claims
    if (!context.Lecturers.Any())
    {
        context.Lecturers.Add(new Lecturer
        {
            LecturerId = 1,
            FirstName = "Owethu",
            LastName = "Ntsholo",
            Email = "owethu@example.com",
            HourlyRate = 250
        });
        context.SaveChanges();
    }
    if (!context.MonthlyClaims.Any())
    {
        if (!context.MonthlyClaims.Any())
        {
            var claims = new List<MonthlyClaim>
            {

            };


            context.MonthlyClaims.AddRange(claims);
            context.SaveChanges();
        }
    }

    // ------------------------------------------------------
    // 6️⃣ Map Default Routes
    // ------------------------------------------------------
    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");

    app.Run();
}