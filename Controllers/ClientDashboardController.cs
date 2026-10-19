using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using HealthTrack.Models;
using HealthTrack.Data; // Added for Database access
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore; // Added for EF Core
using System; // Added for DateTime
using System.Linq; // Added for FirstOrDefault

namespace HealthTrack.Controllers
{
    [Authorize]
    public class ClientDashboardController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context; // Added DbContext

        // Constructor injecting both UserManager and DbContext
        public ClientDashboardController(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        // ---- Existing actions ----
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult HealthRecords()
        {
            return View();
        }
        public IActionResult Workout()
        {
            return View();
        }
        public IActionResult Diet()
        {
            return View();
        }

        public IActionResult Appointments()
        {
            return View();
        }
        public IActionResult Progress()
        {
            return View();
        }

        public IActionResult Notifications()
        {
            var notifications = new List<NotificationItem>
            {
                new NotificationItem { Title = "Workout Reminder", Message = "Don't forget your Chest & Arms workout today!", Time = "10 minutes ago", Icon = "bi-activity", Color = "blue" },
                new NotificationItem { Title = "Appointment Confirmed", Message = "Your session with Trainer Mike is confirmed for tomorrow at 10 AM.", Time = "2 hours ago", Icon = "bi-calendar-check", Color = "green" },
                new NotificationItem { Title = "Diet Plan Updated", Message = "Your trainer has updated your meal plan.", Time = "1 day ago", Icon = "bi-egg-fried", Color = "orange" }
            };
            return View(notifications);
        }

        // ---- Profile Actions ----

        // 1. Show Profile (GET)
        public async Task<IActionResult> ManageProfile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            var roles = await _userManager.GetRolesAsync(user);
            ViewBag.Role = roles.Count > 0 ? roles[0] : "Client";

            // Fetch HealthProfile from database
            var healthProfile = await _context.HealthProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id);

            // If no profile exists yet, create a temporary empty one to show in the view
            if (healthProfile == null)
            {
                healthProfile = new HealthProfile { UserId = user.Id, Weight = 0, Height = 0, DateOfBirth = DateTime.Now.AddYears(-20) };
            }

            // Map both User and HealthProfile data to the ViewModel
            var model = new ManageProfileViewModel
            {
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                ProfilePictureUrl = user.ProfilePictureUrl,
                DateOfBirth = healthProfile.DateOfBirth == default ? DateTime.Now.AddYears(-20) : healthProfile.DateOfBirth,
                Weight = healthProfile.Weight,
                Height = healthProfile.Height,
                BMI = healthProfile.BMI,
                MedicalNotes = healthProfile.MedicalNotes
            };

            return View(model);
        }

        //for uploading profile picture
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadProfilePicture(IFormFile profilePic)
        {
            if (profilePic != null && profilePic.Length > 0)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

                // Allowed extensions
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
                var extension = Path.GetExtension(profilePic.FileName).ToLower();

                if (!allowedExtensions.Contains(extension))
                {
                    return RedirectToAction("ManageProfile"); // Handle error properly in a real app
                }

                // Create unique filename
                var fileName = Guid.NewGuid().ToString() + extension;

                // Define path (wwwroot/uploads/profiles)
                var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "profiles");

                // Ensure directory exists
                if (!Directory.Exists(uploadPath))
                {
                    Directory.CreateDirectory(uploadPath);
                }

                var filePath = Path.Combine(uploadPath, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await profilePic.CopyToAsync(stream);
                }

                // Save filename to database
                user.ProfilePictureUrl = fileName;
                await _userManager.UpdateAsync(user);
            }

            return RedirectToAction("ManageProfile");
        }

        // 2. Save Edited Profile (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(ManageProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // If validation fails, reload the page (you might want to return View with errors later)
                return RedirectToAction("ManageProfile");
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            // Update User Base Data
            user.FullName = model.FullName;
            user.PhoneNumber = model.PhoneNumber;
            await _userManager.UpdateAsync(user);

            // Update or Create Health Profile
            var healthProfile = await _context.HealthProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id);

            if (healthProfile == null)
            {
                healthProfile = new HealthProfile { UserId = user.Id };
                _context.HealthProfiles.Add(healthProfile);
            }

            healthProfile.DateOfBirth = model.DateOfBirth;
            healthProfile.Weight = model.Weight;
            healthProfile.Height = model.Height;

            // Calculate BMI automatically
            if (model.Height > 0)
            {
                decimal heightInMeters = model.Height / 100;
                healthProfile.BMI = model.Weight / (heightInMeters * heightInMeters);
            }
            else
            {
                healthProfile.BMI = 0;
            }

            healthProfile.MedicalNotes = model.MedicalNotes;

            await _context.SaveChangesAsync();

            return RedirectToAction("ManageProfile"); // Reload page after save
        }
    }

    public class NotificationItem
    {
        public string Title { get; set; }
        public string Message { get; set; }
        public string Time { get; set; }
        public string Icon { get; set; }
        public string Color { get; set; }
    }
}