using PROGCMCS.Data;
using PROGCMCS.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace PROGCMCS.Services
{
    public class HRService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public HRService(ApplicationDbContext context, UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task<List<UserListViewModel>> GetAllUsersAsync()
        {
            var users = await _userManager.Users.ToListAsync();
            var result = new List<UserListViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var role = roles.FirstOrDefault() ?? "No Role";

                var lecturer = await _context.Lecturers.FirstOrDefaultAsync(l => l.Email == user.Email);

                result.Add(new UserListViewModel
                {
                    UserId = user.Id,
                    LecturerId = lecturer?.LecturerId ?? 0,
                    FirstName = lecturer?.FirstName ?? user.Email.Split('@')[0],
                    LastName = lecturer?.LastName ?? "",
                    Email = user.Email,
                    Department = lecturer?.Department ?? "N/A",
                    HourlyRate = lecturer?.HourlyRate ?? 0,
                    Role = role,
                    IsActive = lecturer?.IsActive ?? true,
                    LastLogin = null // You can track this if you add login tracking
                });
            }

            return result;
        }

        public async Task<(bool success, string message, string generatedPassword)> CreateUserAsync(CreateUserViewModel model)
        {
            try
            {
                // Check if user already exists
                var existingUser = await _userManager.FindByEmailAsync(model.Email);
                if (existingUser != null)
                {
                    return (false, "User with this email already exists.", "");
                }

                // Generate random password
                var generatedPassword = GenerateRandomPassword();

                // Create Identity user
                var user = new IdentityUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    EmailConfirmed = true
                };

                var result = await _userManager.CreateAsync(user, generatedPassword);
                if (!result.Succeeded)
                {
                    return (false, $"Failed to create user: {string.Join(", ", result.Errors.Select(e => e.Description))}", "");
                }

                // Add to role
                await _userManager.AddToRoleAsync(user, model.Role);

                // Create lecturer profile if role is Lecturer
                if (model.Role == "Lecturer")
                {
                    var lecturer = new Lecturer
                    {
                        FirstName = model.FirstName,
                        LastName = model.LastName,
                        Email = model.Email,
                        Department = model.Department,
                        HourlyRate = model.HourlyRate,
                        IsActive = true
                    };

                    _context.Lecturers.Add(lecturer);
                    await _context.SaveChangesAsync();
                }

                return (true, "User created successfully.", generatedPassword);
            }
            catch (Exception ex)
            {
                return (false, $"Error creating user: {ex.Message}", "");
            }
        }

        public async Task<(bool success, string message)> UpdateUserAsync(EditUserViewModel model)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(model.UserId);
                if (user == null)
                {
                    return (false, "User not found.");
                }

                // Update email if changed
                if (user.Email != model.Email)
                {
                    user.Email = model.Email;
                    user.UserName = model.Email;
                    await _userManager.UpdateAsync(user);
                }

                // Update roles if changed
                var currentRoles = await _userManager.GetRolesAsync(user);
                if (!currentRoles.Contains(model.Role))
                {
                    await _userManager.RemoveFromRolesAsync(user, currentRoles);
                    await _userManager.AddToRoleAsync(user, model.Role);
                }

                // Update lecturer profile
                var lecturer = await _context.Lecturers.FirstOrDefaultAsync(l => l.Email == user.Email);
                if (lecturer != null)
                {
                    lecturer.FirstName = model.FirstName;
                    lecturer.LastName = model.LastName;
                    lecturer.Email = model.Email;
                    lecturer.Department = model.Department;
                    lecturer.HourlyRate = model.HourlyRate;
                    lecturer.IsActive = model.IsActive;
                }
                else if (model.Role == "Lecturer")
                {
                    // Create lecturer profile if it doesn't exist but role is Lecturer
                    lecturer = new Lecturer
                    {
                        FirstName = model.FirstName,
                        LastName = model.LastName,
                        Email = model.Email,
                        Department = model.Department,
                        HourlyRate = model.HourlyRate,
                        IsActive = model.IsActive
                    };
                    _context.Lecturers.Add(lecturer);
                }

                await _context.SaveChangesAsync();
                return (true, "User updated successfully.");
            }
            catch (Exception ex)
            {
                return (false, $"Error updating user: {ex.Message}");
            }
        }

        public async Task<(bool success, string message)> ResetPasswordAsync(string userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return (false, "User not found.");
                }

                var newPassword = GenerateRandomPassword();
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

                if (result.Succeeded)
                {
                    return (true, $"Password reset successfully. New password: {newPassword}");
                }

                return (false, $"Failed to reset password: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }
            catch (Exception ex)
            {
                return (false, $"Error resetting password: {ex.Message}");
            }
        }

        private string GenerateRandomPassword()
        {
            const string validChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890!@#$%";
            var random = new Random();
            var chars = new char[12];

            for (int i = 0; i < chars.Length; i++)
            {
                chars[i] = validChars[random.Next(validChars.Length)];
            }

            return new string(chars);
        }
    }
}