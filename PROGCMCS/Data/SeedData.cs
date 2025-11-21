using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PROGCMCS.Models;

namespace PROGCMCS.Data
{
    public static class SeedData
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();

            // Apply pending migrations
            await context.Database.MigrateAsync();

            // Define roles
            string[] roles = { "Lecturer", "Coordinator", "Manager", "HR" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));
            }

            // Helper method: create user and add to role
            async Task CreateUser(string email, string role, string? first = null, string? last = null,
                string? dept = null, decimal hourlyRate = 0)
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

                    // Use valid passwords with at least one lowercase, uppercase, digit, and special character
                    string password = role switch
                    {
                        "HR" => "Hr@123!",
                        _ => $"{role}@123!"
                    };

                    var result = await userManager.CreateAsync(user, password);
                    if (!result.Succeeded)
                    {
                        var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                        throw new Exception($"Failed to create user {email}: {errors}");
                    }

                    await userManager.AddToRoleAsync(user, role);

                    // If Lecturer, create lecturer profile
                    if (role == "Lecturer" && first != null && last != null && dept != null)
                    {
                        if (!context.Lecturers.Any(l => l.Email == email))
                        {
                            context.Lecturers.Add(new Lecturer
                            {
                                FirstName = first,
                                LastName = last,
                                Email = email,
                                Department = dept,
                                HourlyRate = hourlyRate
                            });
                            await context.SaveChangesAsync();
                        }
                    }
                }
            }

            // Seed users
            await CreateUser("lecturer@university.com", "Lecturer", "John", "Smith", "Computer Science", 85m);
            await CreateUser("lecturer2@university.com", "Lecturer", "Emma", "Davis", "Information Technology", 80m);
            await CreateUser("coordinator@university.com", "Coordinator");
            await CreateUser("manager@university.com", "Manager");
            await CreateUser("hr@university.com", "HR");

            // Seed sample monthly claims for lecturers
            var lecturers = await context.Lecturers.ToListAsync();
            foreach (var lecturer in lecturers)
            {
                int month = DateTime.Now.Month - 1;
                int year = DateTime.Now.Year;

                if (!context.MonthlyClaims.Any(c => c.LecturerId == lecturer.LecturerId && c.Month == month && c.Year == year))
                {
                    context.MonthlyClaims.Add(new MonthlyClaim
                    {
                        LecturerId = lecturer.LecturerId,
                        Month = month,
                        Year = year,
                        TotalHours = lecturer.Email == "lecturer@university.com" ? 10 : 8,
                        HourlyRate = lecturer.HourlyRate,
                        TotalAmount = lecturer.HourlyRate * (lecturer.Email == "lecturer@university.com" ? 10 : 8),
                        Status = ClaimStatus.Submitted,
                        SubmissionDate = DateTime.Now.AddDays(-3)
                    });
                }
            }

            await context.SaveChangesAsync();
        }
    }
}
