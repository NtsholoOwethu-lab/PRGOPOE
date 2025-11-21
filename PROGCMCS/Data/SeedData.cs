using System;
using System.Linq;
using System.Threading.Tasks;
using PROGCMCS.Data;
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
            // Get required services
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
            var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

            logger.LogInformation("Starting seed data...");

            // Define all roles
            string[] roles = { "Lecturer", "Coordinator", "Manager", "HR" };

            // Create roles if they don't exist
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                    logger.LogInformation($"Created role: {role}");
                }
            }

            // ===============================
            // Helper to create users with better error handling
            // ===============================
            async Task<(bool success, IdentityUser user)> CreateUserAsync(string email, string role)
            {
                try
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

                        // Use consistent password pattern that meets requirements
                        var password = $"{role}Password123!";
                        
                        var result = await userManager.CreateAsync(user, password);
                        if (result.Succeeded)
                        {
                            await userManager.AddToRoleAsync(user, role);
                            logger.LogInformation($"Created user: {email} with role: {role}");
                            return (true, user);
                        }
                        else
                        {
                            logger.LogError($"Failed to create user {email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                            return (false, null);
                        }
                    }
                    else
                    {
                        logger.LogInformation($"User {email} already exists");
                        return (true, user);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, $"Error creating user {email}");
                    return (false, null);
                }
            }

            // ===============================
            // Helper to create lecturer with profile
            // ===============================
            async Task<Lecturer?> CreateLecturerUser(string email, string role, string first, string last, string dept, decimal rate)
            {
                var (success, user) = await CreateUserAsync(email, role);
                if (!success || user == null) return null;

                // Create lecturer profile if role is Lecturer
                if (role == "Lecturer")
                {
                    var existingLecturer = await context.Lecturers.FirstOrDefaultAsync(l => l.Email == email);
                    if (existingLecturer == null)
                    {
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
                        logger.LogInformation($"Created lecturer profile for: {email}");
                        return lecturer;
                    }
                    return existingLecturer;
                }

                return null;
            }

            // ===============================
            // Seed all users
            // ===============================
            logger.LogInformation("Creating users...");

            // Create lecturers
            var lecturer1 = await CreateLecturerUser("lecturer@university.com", "Lecturer", "John", "Smith", "Computer Science", 85m);
            var lecturer2 = await CreateLecturerUser("lecturer2@university.com", "Lecturer", "Emma", "Davis", "Information Technology", 80m);

            // Create other users - FIXED: Using the same helper for all users
            await CreateUserAsync("coordinator@university.com", "Coordinator");
            await CreateUserAsync("manager@university.com", "Manager");
            
            // FIXED: HR user creation with proper error handling
            var hrResult = await CreateUserAsync("hr@university.com", "HR");
            if (!hrResult.success)
            {
                logger.LogError("FAILED to create HR user!");
            }

            // ===============================
            // Seed sample monthly claims
            // ===============================
            if (lecturer1 != null || lecturer2 != null)
            {
                void AddClaim(Lecturer? lecturer, int month, int year, decimal hours)
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
                            SubmissionDate = DateTime.Now.AddDays(-3)
                        });
                    }
                }

                AddClaim(lecturer1, DateTime.Now.Month - 1, DateTime.Now.Year, 10);
                AddClaim(lecturer2, DateTime.Now.Month - 1, DateTime.Now.Year, 8);

                await context.SaveChangesAsync();
                logger.LogInformation("Sample claims created");
            }

            logger.LogInformation("Seed data completed");
        }
    }
}