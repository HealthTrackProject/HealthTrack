using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using HealthTrack.Models;
using HealthTrack.Data;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System;
using System.Linq;

namespace HealthTrack.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminDashboardController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        // Constructor to inject UserManager and DbContext
        public AdminDashboardController(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        // --- ADDED THIS MISSING ACTION ---
        // This is the default view when navigating to /AdminDashboard
        public IActionResult Index()
        {
            return View();
        }
        // ---------------------------------

        public IActionResult ManageUser() { return View(); }
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