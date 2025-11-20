using CMCS.Data;
using CMCS.Models;
using CMCS.Services;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

// 1️⃣ Configure Services

builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();

// CSRF protection
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
});

// EF Core SQL Server with retry on failure
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.EnableRetryOnFailure()
    )
);

// Identity
builder.Services.AddIdentity<IdentityUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// Custom Services
builder.Services.AddScoped<AutomationService>();

// 2️⃣ Build the App
var app = builder.Build();

// 3️⃣ Database Initialization & Seeding
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

    // Apply migrations (or use EnsureCreated for quick testing)
    context.Database.Migrate();

    // Create roles if they don't exist
    string[] roles = { "Admin", "HR", "Lecturer", "Verifier", "Approver" };
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));
    }

    // Seed Lecturers
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
        await context.SaveChangesAsync();
    }

    // Seed Monthly Claims (empty list for now)
    if (!context.MonthlyClaims.Any())
    {
        context.MonthlyClaims.AddRange(new List<MonthlyClaim>());
        await context.SaveChangesAsync();
    }
}

// 4️⃣ Configure HTTP Request Pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// 5️⃣ Map Routes
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
);

app.Run();
