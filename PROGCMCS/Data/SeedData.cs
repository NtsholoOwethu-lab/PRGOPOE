using System;
using System.Threading.Tasks;
using PROGCMCS.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace PROGCMCS.Data
{
    public static class SeedData
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();

            await context.Database.MigrateAsync();

            string[] roles = { "Lecturer", "Coordinator", "Manager","HR" };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));
            }

            //
            // 1️⃣ Create Users + Lecturer records
            //

            async Task<Lecturer> CreateLecturerUser(string email, string role, string first, string last, string dept, decimal rate)
            {
                var user = await userManager.FindByEmailAsync(email);
                if (user == null)
                {
                    user = new IdentityUser
                    {
                        UserName = email,
                        Email = email,
                        EmailConfirmed = true
                    };

                    await userManager.CreateAsync(user, $"{role}@123!");
                    await userManager.AddToRoleAsync(user, role);

                    var lecturer = new Lecturer
                    {
                        FirstName = first,
                        LastName = last,
                        Email = email,
                        Department = dept,
                        HourlyRate = rate
                    };

                    context.Lecturers.Add(lecturer);
                    await context.SaveChangesAsync();

                    return lecturer;
                }
                else
                {
                    return await context.Lecturers.FirstOrDefaultAsync(x => x.Email == email);
                }
            }

            var lecturer1 = await CreateLecturerUser("lecturer@university.com", "Lecturer", "John", "Smith", "Computer Science", 85.00m);
            var coordinator = await CreateLecturerUser("coordinator@university.com", "ProgrammeCoordinator", "Sarah", "Johnson", "Computer Science", 0m);
            var manager = await CreateLecturerUser("manager@university.com", "AcademicManager", "David", "Wilson", "Academic Affairs", 0m);
            var lecturer2 = await CreateLecturerUser("lecturer2@university.com", "Lecturer", "Emma", "Davis", "Information Technology", 80.00m);

            //
            // 2️⃣ Seed Monthly Claims
            //

            void AddClaim(Lecturer lecturer, int month, int year, decimal hours)
            {
                if (lecturer == null) return;

                if (!context.MonthlyClaims.Any(c => c.LecturerId == lecturer.LecturerId && c.Month == month && c.Year == year))
                {
                    context.MonthlyClaims.Add(new MonthlyClaim
                    {
                        LecturerId = lecturer.LecturerId,
                        Month = month,
                        Year = year,
                        TotalHours = hours,
                        HourlyRate = lecturer.HourlyRate,
                        TotalAmount = lecturer.HourlyRate * hours,
                        Status = ClaimStatus.Submitted,
                        SubmissionDate = DateTime.Now.AddDays(-5)
                    });
                }
            }

            // Example seeded claims
            AddClaim(lecturer1, DateTime.Now.Month - 1, DateTime.Now.Year, 12);
            AddClaim(lecturer2, DateTime.Now.Month - 1, DateTime.Now.Year, 10);
            AddClaim(coordinator, DateTime.Now.Month - 1, DateTime.Now.Year, 5);
            AddClaim(manager, DateTime.Now.Month - 1, DateTime.Now.Year, 3);

            await context.SaveChangesAsync();
        }
        public static async Task SeedHrUser(IServiceProvider serviceProvider)
        {
            var userManager = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            if (!await roleManager.RoleExistsAsync("HR"))
                await roleManager.CreateAsync(new IdentityRole("HR"));

            string hrEmail = "hr@university.com";
            var hrUser = await userManager.FindByEmailAsync(hrEmail);
            if (hrUser == null)
            {
                hrUser = new IdentityUser { UserName = hrEmail, Email = hrEmail, EmailConfirmed = true };
                await userManager.CreateAsync(hrUser, "HR@123!");
                await userManager.AddToRoleAsync(hrUser, "HR");
            }
        }

    }
}
