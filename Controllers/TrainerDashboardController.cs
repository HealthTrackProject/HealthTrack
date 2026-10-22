using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity; // Added for UserManager
using HealthTrack.Models;            // Added for ApplicationUser, HealthProfile
using HealthTrack.Data;              // Added for ApplicationDbContext
using System.Threading.Tasks;        // Added for async/await
using Microsoft.EntityFrameworkCore; // Added for FirstOrDefaultAsync
using System.IO;                     // Added for File handling
using System;                        // Added for Guid, DateTime
using System.Linq;                   // Added for .Contains()

namespace HealthTrack.Controllers
{
    [Authorize(Roles = "Trainer")] // Only accessible by Trainers
    public class TrainerDashboardController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        // Constructor to inject UserManager and DbContext
        public TrainerDashboardController(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        // ---- Existing Actions ----
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult MyClient() { return View(); }
        public IActionResult WorkoutPlans() { return View(); }
        public IActionResult DietPlans() { return View(); }
        public IActionResult Appointments() { return View(); }
        public IActionResult ClientProgress() { return View(); }
        public IActionResult Feedback() { return View(); }

        public IActionResult Notifications()
        {
            var notifications = new List<NotificationItem>
            {
                new NotificationItem { Title = "New Client Assigned", Message = "You have been assigned to Kamal Perera.", Time = "1 hour ago", Icon = "bi-person-plus", Color = "blue" },
                new NotificationItem { Title = "Appointment Request", Message = "Nimali Silva requested a new appointment for tomorrow.", Time = "3 hours ago", Icon = "bi-calendar-check", Color = "green" },
                new NotificationItem { Title = "Workout Plan Completed", Message = "Sahan Fernando completed his workout plan for this week.", Time = "1 day ago", Icon = "bi-activity", Color = "orange" }
            };
            return View(notifications);
        }

        // ---- Newly Added Profile Actions ----

        // 1. Show Trainer Profile (GET)
        public async Task<IActionResult> MyProfile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            var roles = await _userManager.GetRolesAsync(user);
            ViewBag.Role = roles.Count > 0 ? roles[0] : "Trainer";

            var healthProfile = await _context.HealthProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id);

            if (healthProfile == null)
            {
                healthProfile = new HealthProfile { UserId = user.Id, Weight = 0, Height = 0, DateOfBirth = DateTime.Now.AddYears(-25) };
            }

            var model = new ManageProfileViewModel
            {
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                ProfilePictureUrl = user.ProfilePictureUrl,
                DateOfBirth = healthProfile.DateOfBirth == default ? DateTime.Now.AddYears(-25) : healthProfile.DateOfBirth,
                Weight = healthProfile.Weight,
                Height = healthProfile.Height,
                BMI = healthProfile.BMI,
                MedicalNotes = healthProfile.MedicalNotes
            };

            return View(model);
        }

        // 2. Save Trainer Profile (POST)
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

        // 3. Upload Trainer Profile Picture (POST)
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