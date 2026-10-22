using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using HealthTrack.Models;
using HealthTrack.Data;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.IO;

namespace HealthTrack.Controllers
{
    [Authorize]
    public class ClientDashboardController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public ClientDashboardController(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            // 1. Fetch Health Profile (Weight, BMI)
            var healthProfile = await _context.HealthProfiles.FirstOrDefaultAsync(h => h.UserId == user.Id);

            // 2. Fetch Weekly Workouts & Calories (Last 7 days)
            var startOfWeek = DateTime.Now.AddDays(-7);
            var weeklyLogs = await _context.WorkoutLogs
                .Where(w => w.ClientId == user.Id && w.WorkoutDate >= startOfWeek)
                .ToListAsync();

            // 3. Fetch Upcoming Appointments
            var upcomingAppointments = await _context.Appointments
                .Include(a => a.Trainer)
                .Where(a => a.ClientId == user.Id && a.AppointmentDate >= DateTime.Now && a.Status == "Approved")
                .OrderBy(a => a.AppointmentDate)
                .Take(3)
                .ToListAsync();

            // 4. Fetch Weight History for Chart (Last 6 Health Records)
            var weightHistory = await _context.HealthRecords
                .Where(h => h.ClientId == user.Id)
                .OrderByDescending(h => h.RecordDate)
                .Take(6)
                .ToListAsync();

            weightHistory.Reverse(); // Ascending order for chart

            // --- Map to ViewModel ---
            var model = new ClientDashboardViewModel
            {
                FullName = user.FullName,
                CurrentWeight = healthProfile?.Weight ?? 0,
                CurrentBMI = healthProfile?.BMI ?? 0,
                WeeklyCalories = weeklyLogs.Sum(w => w.CaloriesBurned),
                WeeklyWorkouts = weeklyLogs.Count,

                UpcomingActivities = upcomingAppointments.Select(a => new UpcomingActivity
                {
                    Title = $"Appointment with {a.Trainer?.FullName ?? "Trainer"}",
                    Time = a.AppointmentDate.ToString("dd MMM yyyy, h:mm tt"),
                    Icon = "bi-calendar-event",
                    Color = "purple"
                }).ToList(),

                WeightChartData = weightHistory.Select(h => new ChartDataPoint
                {
                    Label = h.RecordDate.ToString("MMM"),
                    Value = (double)h.Weight
                }).ToList(),

                CaloriesChartData = weeklyLogs
                    .GroupBy(w => w.WorkoutDate.DayOfWeek)
                    .Select(g => new ChartDataPoint
                    {
                        Label = g.Key.ToString().Substring(0, 3),
                        Value = g.Sum(w => w.CaloriesBurned)
                    }).ToList()
            };

            // Fallback for UI demonstration if no data
            if (!model.WeightChartData.Any())
            {
                model.WeightChartData = new List<ChartDataPoint>
                {
                    new ChartDataPoint { Label = "Jan", Value = 70 },
                    new ChartDataPoint { Label = "Feb", Value = 69 }
                };
            }

            // this should pass to the model
            return View(model);
        }
        // ------------------------------------------------

        // 1. Show Health Records Page (GET)
        public async Task<IActionResult> HealthRecords()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            // Get latest Health Profile
            var profile = await _context.HealthProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id);

            // Get all health records for this user
            var records = await _context.HealthRecords
                .Where(r => r.ClientId == user.Id)
                .OrderByDescending(r => r.RecordDate)
                .ToListAsync();

            var model = new HealthRecordsViewModel
            {
                CurrentWeight = profile?.Weight ?? 0,
                CurrentHeight = profile?.Height ?? 0,
                CurrentBMI = profile?.BMI ?? 0,
                Records = records
            };

            return View(model);
        }

        // 2. Add New Record (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddHealthRecord(decimal weight, decimal height, int caloriesBurned, DateTime recordDate)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            // Calculate BMI
            decimal bmi = 0;
            if (height > 0)
            {
                decimal heightInMeters = height / 100;
                bmi = weight / (heightInMeters * heightInMeters);
            }

            // Save to HealthRecords Table
            var newRecord = new HealthRecord
            {
                ClientId = user.Id,
                Weight = weight,
                Height = height,
                BMI = bmi,
                CaloriesBurned = caloriesBurned,
                RecordDate = recordDate
            };

            _context.HealthRecords.Add(newRecord);

            // Update the Health Profile so the top summary cards change
            var profile = await _context.HealthProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id);
            if (profile != null)
            {
                profile.Weight = weight;
                profile.Height = height;
                profile.BMI = bmi;
            }
            else
            {
                _context.HealthProfiles.Add(new HealthProfile
                {
                    UserId = user.Id,
                    Weight = weight,
                    Height = height,
                    BMI = bmi,
                    DateOfBirth = DateTime.Now.AddYears(-20)
                });
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("HealthRecords");
        }

        // 3. Delete Record (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteHealthRecord(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            var record = await _context.HealthRecords.FirstOrDefaultAsync(r => r.HealthRecordId == id && r.ClientId == user.Id);
            if (record != null)
            {
                _context.HealthRecords.Remove(record);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("HealthRecords");
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

        public async Task<IActionResult> ManageProfile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            var roles = await _userManager.GetRolesAsync(user);
            ViewBag.Role = roles.Count > 0 ? roles[0] : "Client";

            var healthProfile = await _context.HealthProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id);

            if (healthProfile == null)
            {
                healthProfile = new HealthProfile { UserId = user.Id, Weight = 0, Height = 0, DateOfBirth = DateTime.Now.AddYears(-20) };
            }

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

                if (!allowedExtensions.Contains(extension))
                {
                    return RedirectToAction("ManageProfile");
                }

                var fileName = Guid.NewGuid().ToString() + extension;
                var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "profiles");

                if (!Directory.Exists(uploadPath))
                {
                    Directory.CreateDirectory(uploadPath);
                }

                var filePath = Path.Combine(uploadPath, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await profilePic.CopyToAsync(stream);
                }

                user.ProfilePictureUrl = fileName;
                await _userManager.UpdateAsync(user);
            }

            return RedirectToAction("ManageProfile");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(ManageProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return RedirectToAction("ManageProfile");
            }

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
            else
            {
                healthProfile.BMI = 0;
            }

            healthProfile.MedicalNotes = model.MedicalNotes;

            await _context.SaveChangesAsync();

            return RedirectToAction("ManageProfile");
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