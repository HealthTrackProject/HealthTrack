using HealthTrack.Data;
using HealthTrack.Models;
using HealthTrack.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace HealthTrack.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminDashboardController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        // Constructor to inject UserManager and DbContext
        public AdminDashboardController(UserManager<ApplicationUser> userManager, ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _userManager = userManager;
            _context = context;
            _environment = environment;
        }

        // --- ADDED THIS MISSING ACTION ---
        // This is the default view when navigating to /AdminDashboard
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var model = new AdminDashboardViewModel();

            // 1. Total registered users
            model.TotalUsers = await _context.Users
                .CountAsync();

            // 2. Count users by Identity role
            model.TotalAdmins = (await _userManager
                .GetUsersInRoleAsync("Admin")).Count;

            model.TotalTrainers = (await _userManager
                .GetUsersInRoleAsync("Trainer")).Count;

            model.TotalClients = (await _userManager
                .GetUsersInRoleAsync("Client")).Count;

            // 3. Total appointments
            model.TotalAppointments = await _context.Appointments
                .CountAsync();

            // 4. Recent appointments
            var appointments = await _context.Appointments
                .AsNoTracking()
                .OrderByDescending(a => a.AppointmentDate)
                .Take(5)
                .Select(a => new AdminActivityViewModel
                {
                    Title = "Appointment " + a.Status,
                    Name = a.Client != null
                        ? a.Client.FullName
                        : "Unknown client",
                    Date = a.AppointmentDate,
                    Type = "appointment"
                })
                .ToListAsync();

            // 5. Recent workout plans
            var workoutPlans = await _context.WorkoutPlans
                .AsNoTracking()
                .OrderByDescending(w => w.StartDate)
                .Take(5)
                .Select(w => new AdminActivityViewModel
                {
                    Title = "Workout plan created",
                    Name = w.Client != null
                        ? w.Client.FullName
                        : "Unknown client",
                    Date = w.StartDate,
                    Type = "workout"
                })
                .ToListAsync();

            // 6. Combine and sort activities
            model.RecentActivities = appointments
                .Concat(workoutPlans)
                .OrderByDescending(a => a.Date)
                .Take(6)
                .ToList();

            return View(model);
        }
        // ---------------------------------

        // GET: /AdminDashboard/ManageUser
        // Admins and Trainers can view the user list.
        [HttpGet]
        [Authorize(Roles = "Admin,Trainer")]
        public async Task<IActionResult> ManageUser()
        {
            var users = await _userManager.Users
                .AsNoTracking()
                .OrderBy(u => u.FullName)
                .ToListAsync();

            var model = new ManageUsersViewModel();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);

                model.Users.Add(new UserRowViewModel
                {
                    Id = user.Id,
                    FullName = user.FullName ?? user.UserName ?? "Unknown user",
                    Email = user.Email ?? "",
                    Role = roles.FirstOrDefault() ?? "Client",
                    ProfileImage = user.ProfilePictureUrl
                });
            }

            return View(model);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ManageUser(ManageUsersViewModel model)
        {
            if (!User.IsInRole("Admin"))
            {
                TempData["ToastType"] = "error";
                TempData["ToastMessage"] = "You are not authorized to add users.";
                return RedirectToAction(nameof(ManageUser));
            }

            if (model.NewUser == null)
            {
                TempData["ToastType"] = "error";
                TempData["ToastMessage"] = "Please provide the user details.";
                return RedirectToAction(nameof(ManageUser));
            }

            // ONLY validate the NewUser properties, ignoring the EditUser properties
            ModelState.Clear();
            if (!TryValidateModel(model.NewUser, nameof(model.NewUser)))
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                TempData["ToastType"] = "error";
                TempData["ToastMessage"] = string.Join(" ", errors);
                return RedirectToAction(nameof(ManageUser));
            }

            var input = model.NewUser;

            if (input.Role != "Admin" && input.Role != "Trainer")
            {
                TempData["ToastType"] = "error";
                TempData["ToastMessage"] = "Admins can create only Admin or Trainer accounts.";
                return RedirectToAction(nameof(ManageUser));
            }

            var existingUser = await _userManager.FindByEmailAsync(input.Email);

            if (existingUser != null)
            {
                TempData["ToastType"] = "error";
                TempData["ToastMessage"] = "This email address is already registered.";
                return RedirectToAction(nameof(ManageUser));
            }

            var user = new ApplicationUser
            {
                UserName = input.Email.Trim(),
                Email = input.Email.Trim(),
                FullName = input.FullName.Trim(),
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, input.Password);

            if (!result.Succeeded)
            {
                TempData["ToastType"] = "error";
                TempData["ToastMessage"] = string.Join(" ", result.Errors.Select(e => e.Description));
                return RedirectToAction(nameof(ManageUser));
            }

            var roleResult = await _userManager.AddToRoleAsync(user, input.Role);

            if (!roleResult.Succeeded)
            {
                await _userManager.DeleteAsync(user);
                TempData["ToastType"] = "error";
                TempData["ToastMessage"] = string.Join(" ", roleResult.Errors.Select(e => e.Description));
                return RedirectToAction(nameof(ManageUser));
            }

            TempData["ToastType"] = "success";
            TempData["ToastMessage"] = "User created successfully.";

            return RedirectToAction(nameof(ManageUser));
        }
        // POST: /AdminDashboard/EditUser
        // Only Admin can edit user details.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> EditUser(
            ManageUsersViewModel model,
            IFormFile? editProfileImage)
        {
            var edit = model.EditUser;

            if (string.IsNullOrWhiteSpace(edit.Id) ||
                string.IsNullOrWhiteSpace(edit.FullName) ||
                string.IsNullOrWhiteSpace(edit.Email))
            {
                TempData["ToastMessage"] =
                    "Please provide the required user details.";
                TempData["ToastType"] = "error";

                return RedirectToAction(nameof(ManageUser));
            }

            if (!new EmailAddressAttribute().IsValid(edit.Email))
            {
                TempData["ToastMessage"] =
                    "Please enter a valid email address.";
                TempData["ToastType"] = "error";

                return RedirectToAction(nameof(ManageUser));
            }

            var user = await _userManager.FindByIdAsync(edit.Id);

            if (user == null)
            {
                TempData["ToastMessage"] = "User not found.";
                TempData["ToastType"] = "error";

                return RedirectToAction(nameof(ManageUser));
            }

            // Prevent accidentally removing the last Admin.
            var roles = await _userManager.GetRolesAsync(user);

            if (roles.Contains("Admin") &&
                user.Id == _userManager.GetUserId(User))
            {
                // The current Admin may update their details,
                // but cannot change the role through this form.
            }

            var email = edit.Email.Trim();
            var existingEmailUser = await _userManager.FindByEmailAsync(email);

            if (existingEmailUser != null &&
                existingEmailUser.Id != user.Id)
            {
                TempData["ToastMessage"] =
                    "Another user already uses this email address.";
                TempData["ToastType"] = "warning";

                return RedirectToAction(nameof(ManageUser));
            }

            string? newImagePath = null;

            if (editProfileImage != null && editProfileImage.Length > 0)
            {
                if (!IsValidImage(editProfileImage))
                {
                    TempData["ToastMessage"] =
                        "Please upload a valid JPG, PNG, or WebP image.";
                    TempData["ToastType"] = "error";

                    return RedirectToAction(nameof(ManageUser));
                }

                newImagePath = await SaveProfileImageAsync(editProfileImage);
            }

            var oldImagePath = user.ProfilePictureUrl;

            user.FullName = edit.FullName.Trim();
            user.Email = email;
            user.UserName = email;
            user.NormalizedEmail = _userManager.NormalizeEmail(email);
            user.NormalizedUserName = _userManager.NormalizeName(email);

            if (newImagePath != null)
            {
                user.ProfilePictureUrl = newImagePath;
            }

            var updateResult = await _userManager.UpdateAsync(user);

            if (!updateResult.Succeeded)
            {
                DeleteProfileImage(newImagePath);

                TempData["ToastMessage"] = string.Join(
                    " ",
                    updateResult.Errors.Select(e => e.Description));

                TempData["ToastType"] = "error";

                return RedirectToAction(nameof(ManageUser));
            }

            if (newImagePath != null)
            {
                DeleteProfileImage(oldImagePath);
            }

            TempData["ToastMessage"] = "User updated successfully.";
            TempData["ToastType"] = "success";

            return RedirectToAction(nameof(ManageUser));
        }


        // Only Admin can delete a user.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                TempData["ToastMessage"] = "Invalid user.";
                TempData["ToastType"] = "error";

                return RedirectToAction(nameof(ManageUser));
            }

            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
            {
                TempData["ToastMessage"] = "User not found.";
                TempData["ToastType"] = "error";

                return RedirectToAction(nameof(ManageUser));
            }

            // Do not allow the signed-in Admin to delete their own account here.
            if (user.Id == _userManager.GetUserId(User))
            {
                TempData["ToastMessage"] =
                    "You cannot delete your own account here.";
                TempData["ToastType"] = "warning";

                return RedirectToAction(nameof(ManageUser));
            }

            var imagePath = user.ProfilePictureUrl;
            var result = await _userManager.DeleteAsync(user);

            if (!result.Succeeded)
            {
                TempData["ToastMessage"] = string.Join(
                    " ",
                    result.Errors.Select(e => e.Description));

                TempData["ToastType"] = "error";

                return RedirectToAction(nameof(ManageUser));
            }

            DeleteProfileImage(imagePath);

            TempData["ToastMessage"] = "User deleted successfully.";
            TempData["ToastType"] = "success";

            return RedirectToAction(nameof(ManageUser));
        }


        // Validate and save profile images.
        private static bool IsValidImage(IFormFile file)
        {
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };

            return file.Length <= 5 * 1024 * 1024 &&
                   allowedExtensions.Contains(extension) &&
                   file.ContentType.StartsWith(
                       "image/",
                       StringComparison.OrdinalIgnoreCase);
        }


        private async Task<string> SaveProfileImageAsync(IFormFile file)
        {
            var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "profiles");
            Directory.CreateDirectory(uploadsFolder);

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var fileName = $"{Guid.NewGuid():N}{extension}";
            var fullPath = Path.Combine(uploadsFolder, fileName);

            await using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Change: Return just the filename to match your older database records
            return fileName;
        }

        private void DeleteProfileImage(string? imagePath)
        {
            if (string.IsNullOrWhiteSpace(imagePath))
                return;

            // Only delete files from the application's profile-upload directory.
            var relativePath = imagePath.TrimStart('/')
                .Replace('/', Path.DirectorySeparatorChar);

            var uploadsRoot = Path.GetFullPath(
                Path.Combine(_environment.WebRootPath, "uploads", "profiles"));

            var fullPath = Path.GetFullPath(
                Path.Combine(_environment.WebRootPath, relativePath));

            if (!fullPath.StartsWith(
                    uploadsRoot + Path.DirectorySeparatorChar,
                    StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (System.IO.File.Exists(fullPath))
            {
                System.IO.File.Delete(fullPath);
            }
        }
        public IActionResult ManageClient() { return View(); }
        public IActionResult ManageTrainers() { return View(); }
        public IActionResult Appointments() { return View(); }
        public IActionResult Reports() { return View(); }
        public IActionResult SystemSettings() { return View(); }
        public IActionResult Notifications() { return View(); }

        // ---- Profile Actions ----

        // 1. Show Admin Profile
        public async Task<IActionResult> MyProfile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            var roles = await _userManager.GetRolesAsync(user);
            ViewBag.Role = roles.Count > 0 ? roles[0] : "Admin";

            var healthProfile = await _context.HealthProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id);

            if (healthProfile == null)
            {
                healthProfile = new HealthProfile { UserId = user.Id, Weight = 0, Height = 0, DateOfBirth = DateTime.Now.AddYears(-30) };
            }

            var model = new ManageProfileViewModel
            {
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                ProfilePictureUrl = user.ProfilePictureUrl,
                DateOfBirth = healthProfile.DateOfBirth == default ? DateTime.Now.AddYears(-30) : healthProfile.DateOfBirth,
                Weight = healthProfile.Weight,
                Height = healthProfile.Height,
                BMI = healthProfile.BMI,
                MedicalNotes = healthProfile.MedicalNotes
            };

            return View(model);
        }

        // 2. Save Admin Profile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(ManageProfileViewModel model)
        {
            if (!ModelState.IsValid) return RedirectToAction("MyProfile");

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            user.FullName = model.FullName;
            user.PhoneNumber = model.PhoneNumber;
            await _userManager.UpdateAsync(user);

            var healthProfile = await _context.HealthProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id);
            if (healthProfile == null)
            {
                healthProfile = new HealthProfile { UserId = user.Id };
                _context.HealthProfiles.Add(healthProfile);
            }

            healthProfile.DateOfBirth = model.DateOfBirth;
            healthProfile.Weight = model.Weight;
            healthProfile.Height = model.Height;

            if (model.Height > 0)
            {
                decimal heightInMeters = model.Height / 100;
                healthProfile.BMI = model.Weight / (heightInMeters * heightInMeters);
            }
            else { healthProfile.BMI = 0; }

            healthProfile.MedicalNotes = model.MedicalNotes;

            await _context.SaveChangesAsync();
            return RedirectToAction("MyProfile");
        }

        // 3. Upload Admin Profile Picture
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadProfilePicture(IFormFile profilePic)
        {
            if (profilePic != null && profilePic.Length > 0)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
                var extension = Path.GetExtension(profilePic.FileName).ToLower();

                if (!allowedExtensions.Contains(extension)) return RedirectToAction("MyProfile");

                var fileName = Guid.NewGuid().ToString() + extension;
                var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "profiles");

                if (!Directory.Exists(uploadPath)) Directory.CreateDirectory(uploadPath);

                var filePath = Path.Combine(uploadPath, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await profilePic.CopyToAsync(stream);
                }

                user.ProfilePictureUrl = fileName;
                await _userManager.UpdateAsync(user);
            }

            return RedirectToAction("MyProfile");
        }
    }
}