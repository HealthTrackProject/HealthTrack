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
            // 1. Get the logged-in client.
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return RedirectToPage("/Account/Login", new { area = "Identity" });
            }

            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);
            var now = DateTime.Now;

            // 2. Find the current week's Monday and next Monday.
            int daysSinceMonday = ((int)today.DayOfWeek + 6) % 7;

            DateTime monday = today.AddDays(-daysSinceMonday);
            DateTime nextMonday = monday.AddDays(7);

            // 3. Get this client's workout logs for Monday to Sunday.
            var weeklyWorkoutLogs = await _context.WorkoutLogs
                .AsNoTracking()
                .Where(w =>
                    w.ClientId == user.Id &&
                    w.WorkoutDate >= monday &&
                    w.WorkoutDate < nextMonday)
                .ToListAsync();

            // 4. Calculate calories and workout count from actual workout logs.
            decimal totalCalories = weeklyWorkoutLogs
                .Sum(w => Convert.ToDecimal(w.CaloriesBurned));

            int weeklyCalories = (int)Math.Round(totalCalories);

            int weeklyWorkouts = weeklyWorkoutLogs.Count;

            // 5. Get the client's health records.
            var healthRecords = await _context.HealthRecords
                .AsNoTracking()
                .Where(h => h.ClientId == user.Id)
                .OrderByDescending(h => h.RecordDate)
                .ToListAsync();

            var latestHealth = healthRecords.FirstOrDefault();

            // 6. Use the latest health record first.
            var healthProfile = await _context.HealthProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(h => h.UserId == user.Id);

            decimal currentWeight = latestHealth?.Weight
                ?? healthProfile?.Weight
                ?? 0m;

            decimal currentBMI = latestHealth?.BMI
                ?? healthProfile?.BMI
                ?? 0m;

            // 7. Get upcoming appointments for this client.
            var upcomingAppointments = await _context.Appointments
                .AsNoTracking()
                .Include(a => a.Trainer)
                .Where(a =>
                    a.ClientId == user.Id &&
                    a.AppointmentDate >= now &&
                    (a.Status == "Pending" ||
                     a.Status == "Approved" ||
                     a.Status == "Confirmed"))
                .OrderBy(a => a.AppointmentDate)
                .Take(3)
                .ToListAsync();

            // 8. Prepare the weight progress graph.
            var weightHistory = healthRecords
                .OrderByDescending(h => h.RecordDate)
                .Take(6)
                .OrderBy(h => h.RecordDate)
                .ToList();

            // 9. Prepare the weekly calorie graph using the same workout logs.
            var caloriesChartData = GetWeeklyCaloriesChartData(
                weeklyWorkoutLogs,
                monday);

            // 10. Populate the dashboard view model.
            var model = new ClientDashboardViewModel
            {
                FullName = string.IsNullOrWhiteSpace(user.FullName)
                    ? (user.UserName ?? "Client")
                    : user.FullName,

                CurrentWeight = currentWeight,
                CurrentBMI = currentBMI,

                WeeklyCalories = weeklyCalories,
                WeeklyWorkouts = weeklyWorkouts,

                UpcomingActivities = upcomingAppointments
                    .Select(a => new UpcomingActivity
                    {
                        Title = $"Appointment with {a.Trainer?.FullName ?? "Trainer"}",
                        Time = a.AppointmentDate.ToString("dd MMM yyyy, h:mm tt"),
                        Icon = "bi-calendar-event",
                        Color = "purple"
                    })
                    .ToList(),

                WeightChartData = weightHistory
                    .Select(h => new ChartDataPoint
                    {
                        Label = h.RecordDate.ToString("dd MMM"),
                        Value = (double)h.Weight
                    })
                    .ToList(),

                CaloriesChartData = caloriesChartData
            };

            return View(model);
        }
        private List<ChartDataPoint> GetWeeklyCaloriesChartData(
    List<WorkoutLog> logs,
    DateTime monday)
        {
            var chartData = new List<ChartDataPoint>();

            // Monday through Sunday of the current week.
            for (int i = 0; i < 7; i++)
            {
                DateTime date = monday.Date.AddDays(i);

                decimal dailyCalories = logs
                    .Where(w => w.WorkoutDate.Date == date)
                    .Sum(w => Convert.ToDecimal(w.CaloriesBurned));

                chartData.Add(new ChartDataPoint
                {
                    Label = date.ToString("ddd"),
                    Value = (double)dailyCalories
                });
            }

            return chartData;
        }
        // ---- Health Records Actions ----
        public async Task<IActionResult> HealthRecords()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            var profile = await _context.HealthProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id);

            var records = await _context.HealthRecords
                .Where(r => r.ClientId == user.Id)
                .OrderByDescending(r => r.RecordDate)
                .ToListAsync();

            var latestRecord = records.FirstOrDefault();

            var model = new HealthRecordsViewModel
            {
                CurrentWeight = latestRecord?.Weight ?? profile?.Weight ?? 0,
                CurrentHeight = latestRecord?.Height ?? profile?.Height ?? 0,
                CurrentBMI = latestRecord?.BMI ?? profile?.BMI ?? 0,
                LastHeartRate = latestRecord?.HeartRate ?? 0,
                Records = records
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddHealthRecord(decimal weight, decimal height, int caloriesBurned, DateTime recordDate, int? heartRate, string? bloodPressure)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            decimal bmi = 0;
            if (height > 0)
            {
                // Height values in the current database are stored in metres (e.g. 1.75).
                var heightInMeters = height > 3 ? height / 100 : height;
                bmi = weight / (heightInMeters * heightInMeters);
            }

            var newRecord = new HealthRecord
            {
                ClientId = user.Id,
                Weight = weight,
                Height = height,
                BMI = bmi,
                CaloriesBurned = caloriesBurned,
                RecordDate = recordDate,
                HeartRate = heartRate,
                BloodPressure = bloodPressure
            };

            _context.HealthRecords.Add(newRecord);

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

        // ---- Workout Actions ----
        public async Task<IActionResult> Workout()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToPage("/Account/Login", new { area = "Identity" });

            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            var workoutHistory = await _context.WorkoutLogs
                .AsNoTracking()
                .Where(w => w.ClientId == user.Id)
                .OrderByDescending(w => w.WorkoutDate)
                .ToListAsync();

            // Load ALL plans active today. Do not use FirstOrDefault(), which hides
            // the other plans assigned to this client.
            var assignedPlans = await _context.WorkoutPlans
                .AsNoTracking()
                .Where(p => p.ClientId == user.Id &&
                            p.StartDate < tomorrow &&
                            p.EndDate >= today)
                .OrderBy(p => p.StartDate)
                .ToListAsync();

            var currentPlan = assignedPlans.Select(plan => new WorkoutDay
            {
                Day = today.ToString("dddd"),
                MuscleGroup = plan.PlanName,
                Exercises = string.IsNullOrWhiteSpace(plan.Description)
                    ? "Please follow the instructions provided by your trainer."
                    : plan.Description
            }).ToList();

            var model = new WorkoutViewModel
            {
                WorkoutHistory = workoutHistory,
                CurrentPlan = currentPlan
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LogWorkout(string workoutName, string workoutGroup, string workoutExercises, int workoutDuration, int? workoutCalories, DateTime workoutDate)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            var plan = await _context.WorkoutPlans.FirstOrDefaultAsync(p => p.ClientId == user.Id);

            if (plan == null)
            {
                var trainer = await _userManager.GetUsersInRoleAsync("Trainer");
                var defaultTrainerId = trainer.FirstOrDefault()?.Id ?? user.Id;

                plan = new WorkoutPlan
                {
                    ClientId = user.Id,
                    TrainerId = defaultTrainerId,
                    PlanName = "General Fitness",
                    Description = "Auto-generated general plan",
                    StartDate = DateTime.Now,
                    EndDate = DateTime.Now.AddMonths(1)
                };
                _context.WorkoutPlans.Add(plan);
                await _context.SaveChangesAsync();
            }

            var newLog = new WorkoutLog
            {
                ClientId = user.Id,
                WorkoutPlanId = plan.WorkoutPlanId,
                ExerciseName = $"{workoutName} ({workoutGroup}) - {workoutExercises}",
                DurationMinutes = workoutDuration,
                CaloriesBurned = workoutCalories ?? 0,
                WorkoutDate = workoutDate
            };

            _context.WorkoutLogs.Add(newLog);
            await _context.SaveChangesAsync();

            return RedirectToAction("Workout");
        }

        // ---- Diet Actions ----
        public async Task<IActionResult> Diet()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            var today = DateTime.Today;
            var mealHistory = await _context.DietLogs.AsNoTracking()
                .Where(d => d.ClientId == user.Id)
                .OrderByDescending(d => d.MealDate)
                .ToListAsync();

            // Display all assigned diet plans active today; FirstOrDefault() hid the other plans.
            var assignedPlans = await _context.DietPlans.AsNoTracking()
                .Where(p => p.ClientId == user.Id &&
                            p.StartDate < today.AddDays(1) &&
                            p.EndDate >= today)
                .OrderBy(p => p.StartDate)
                .ToListAsync();

            var foodImages = new[]
            {
                "https://images.unsplash.com/photo-1512621776951-a57141f2eefd?auto=format&fit=crop&w=480&q=80",
                "https://images.unsplash.com/photo-1547592180-85f173990554?auto=format&fit=crop&w=480&q=80",
                "https://images.unsplash.com/photo-1490645935967-10de6ba17061?auto=format&fit=crop&w=480&q=80",
                "https://images.unsplash.com/photo-1546069901-ba9599a7e63c?auto=format&fit=crop&w=480&q=80"
            };

            var todayPlan = assignedPlans.Select((plan, index) => new MockMeal
            {
                Name = plan.PlanName,
                Time = $"Assigned plan • {plan.StartDate:dd MMM} – {plan.EndDate:dd MMM}",
                Food = string.IsNullOrWhiteSpace(plan.Description)
                    ? "No description was added by your trainer."
                    : plan.Description,
                Calories = 0,
                ImageUrl = foodImages[index % foodImages.Length]
            }).ToList();

            return View(new ClientDietViewModel
            {
                MealHistory = mealHistory,
                TodayPlan = todayPlan
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LogMeal(string mealType, string foodDescription, int calories, DateTime mealDate)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            var plan = await _context.DietPlans.FirstOrDefaultAsync(p => p.ClientId == user.Id);
            if (plan == null)
            {
                var trainer = await _userManager.GetUsersInRoleAsync("Trainer");
                var defaultTrainerId = trainer.FirstOrDefault()?.Id ?? user.Id;

                plan = new DietPlan
                {
                    ClientId = user.Id,
                    TrainerId = defaultTrainerId,
                    PlanName = "General Diet",
                    StartDate = DateTime.Now,
                    EndDate = DateTime.Now.AddMonths(1)
                };
                _context.DietPlans.Add(plan);
                await _context.SaveChangesAsync();
            }

            var newLog = new DietLog
            {
                ClientId = user.Id,
                DietPlanId = plan.DietPlanId,
                MealType = mealType,
                FoodDescription = foodDescription,
                Calories = calories,
                MealDate = mealDate
            };

            _context.DietLogs.Add(newLog);
            await _context.SaveChangesAsync();

            return RedirectToAction("Diet");
        }

        // ---- Appointments Actions ----
        public async Task<IActionResult> Appointments()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            var appointments = await _context.Appointments
                .Include(a => a.Trainer)
                .Where(a => a.ClientId == user.Id)
                .OrderBy(a => a.AppointmentDate)
                .ToListAsync();

            var now = DateTime.Now;
            var trainers = await _userManager.GetUsersInRoleAsync("Trainer");

            var model = new ClientAppointmentsViewModel
            {
                UpcomingAppointments = appointments.Where(a => a.AppointmentDate >= now).ToList(),
                PastAppointments = appointments.Where(a => a.AppointmentDate < now).OrderByDescending(a => a.AppointmentDate).ToList(),
                AvailableTrainers = trainers.ToList()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestAppointment(DateTime appointmentDate, string appointmentTime, string preferredTrainerId, string reason)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            var dateStr = appointmentDate.ToString("yyyy-MM-dd");
            var timeStr = appointmentTime;
            DateTime finalDateTime = DateTime.Parse($"{dateStr} {timeStr}");

            if (string.IsNullOrEmpty(preferredTrainerId))
            {
                var trainers = await _userManager.GetUsersInRoleAsync("Trainer");
                preferredTrainerId = trainers.FirstOrDefault()?.Id;

                if (preferredTrainerId == null) return RedirectToAction("Appointments");
            }

            var newAppointment = new Appointment
            {
                ClientId = user.Id,
                TrainerId = preferredTrainerId,
                AppointmentDate = finalDateTime,
                Reason = reason,
                Status = "Pending"
            };

            _context.Appointments.Add(newAppointment);
            await _context.SaveChangesAsync();

            // --- NEW NOTIFICATION LOGIC ADDED HERE ---
            var newNotification = new Notification
            {
                UserId = user.Id,
                Title = "Appointment Requested",
                Message = $"Your appointment request for {finalDateTime:dd MMM yyyy} at {finalDateTime:hh:mm tt} has been received and is pending approval.",
                CreatedAt = DateTime.Now,
                IsRead = false
            };
            _context.Notifications.Add(newNotification);
            await _context.SaveChangesAsync();
            // -----------------------------------------

            return RedirectToAction("Appointments");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelAppointment(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            var appointment = await _context.Appointments.FirstOrDefaultAsync(a => a.AppointmentId == id && a.ClientId == user.Id);

            if (appointment != null)
            {
                appointment.Status = "Cancelled";
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Appointments");
        }

        // ---- Progress Actions ----
        public async Task<IActionResult> Progress()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            var progressRecords = await _context.ProgressRecords.AsNoTracking()
                .Where(p => p.ClientId == user.Id).OrderBy(p => p.RecordDate).ToListAsync();
            var healthRecords = await _context.HealthRecords.AsNoTracking()
                .Where(h => h.ClientId == user.Id).OrderBy(h => h.RecordDate).ToListAsync();
            var workoutLogs = await _context.WorkoutLogs.AsNoTracking()
                .Where(w => w.ClientId == user.Id).OrderBy(w => w.WorkoutDate).ToListAsync();
            var profile = await _context.HealthProfiles.AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == user.Id);

            var latestHealth = healthRecords.LastOrDefault();
            var model = new ProgressViewModel
            {
                CurrentWeight = latestHealth?.Weight ?? profile?.Weight ?? 0,
                CurrentBMI = latestHealth?.BMI ?? profile?.BMI ?? 0,
                TotalCaloriesBurned = workoutLogs.Count > 0
                    ? workoutLogs.Sum(w => w.CaloriesBurned)
                    : progressRecords.Sum(p => p.CaloriesBurned),
                TotalWorkouts = workoutLogs.Count > 0
                    ? workoutLogs.Count
                    : progressRecords.Sum(p => p.WorkoutsCompleted)
            };

            if (healthRecords.Count > 1)
            {
                var change = healthRecords.Last().Weight - healthRecords.First().Weight;
                model.WeightChange = change > 0 ? $"+{change:0.0} kg" : $"{change:0.0} kg";
            }
            else model.WeightChange = "0.0 kg";

            var firstMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(-11);
            for (int i = 0; i < 12; i++)
            {
                var monthStart = firstMonth.AddMonths(i);
                var nextMonth = monthStart.AddMonths(1);
                model.Labels.Add(monthStart.ToString("MMM"));

                var monthHealth = healthRecords.Where(h => h.RecordDate >= monthStart && h.RecordDate < nextMonth)
                    .OrderByDescending(h => h.RecordDate).FirstOrDefault();
                var monthProgress = progressRecords.Where(p => p.RecordDate >= monthStart && p.RecordDate < nextMonth)
                    .OrderByDescending(p => p.RecordDate).FirstOrDefault();
                var monthWorkouts = workoutLogs.Where(w => w.WorkoutDate >= monthStart && w.WorkoutDate < nextMonth).ToList();

                model.WeightData.Add((double)(monthHealth?.Weight ?? monthProgress?.Weight ?? 0));
                model.BMIData.Add((double)(monthHealth?.BMI ?? monthProgress?.BMI ?? 0));
                var monthProgressRecords = progressRecords
                    .Where(p => p.RecordDate >= monthStart && p.RecordDate < nextMonth)
                    .ToList();

                model.CaloriesData.Add(monthWorkouts.Count > 0
                    ? monthWorkouts.Sum(w => w.CaloriesBurned)
                    : monthProgressRecords.Sum(p => p.CaloriesBurned));
                model.WorkoutsData.Add(monthWorkouts.Count > 0
                    ? monthWorkouts.Count
                    : monthProgressRecords.Sum(p => p.WorkoutsCompleted));
            }

            return View(model);
        }

        // ---- Notifications Actions ----
        public async Task<IActionResult> Notifications()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            var dbNotifications = await _context.Notifications
                .Where(n => n.UserId == user.Id)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            var notifications = dbNotifications.Select(n => new NotificationItem
            {
                Title = n.Title,
                Message = n.Message,
                Time = GetRelativeTime(n.CreatedAt),

                Icon = n.Title.ToLower().Contains("workout") ? "bi-activity" :
                       n.Title.ToLower().Contains("appointment") ? "bi-calendar-check" :
                       n.Title.ToLower().Contains("diet") || n.Title.ToLower().Contains("meal") ? "bi-egg-fried" : "bi-bell",

                Color = n.Title.ToLower().Contains("workout") ? "blue" :
                        n.Title.ToLower().Contains("appointment") ? "green" :
                        n.Title.ToLower().Contains("diet") || n.Title.ToLower().Contains("meal") ? "orange" : "purple"
            }).ToList();

            return View(notifications);
        }

        private string GetRelativeTime(DateTime dateTime)
        {
            var timeSpan = DateTime.Now.Subtract(dateTime);

            if (timeSpan <= TimeSpan.FromSeconds(60)) return "just now";
            if (timeSpan <= TimeSpan.FromMinutes(60)) return timeSpan.Minutes > 1 ? $"{timeSpan.Minutes} minutes ago" : "1 minute ago";
            if (timeSpan <= TimeSpan.FromHours(24)) return timeSpan.Hours > 1 ? $"{timeSpan.Hours} hours ago" : "1 hour ago";
            if (timeSpan <= TimeSpan.FromDays(30)) return timeSpan.Days > 1 ? $"{timeSpan.Days} days ago" : "1 day ago";

            return dateTime.ToString("dd MMM yyyy");
        }

        // ---- Profile Actions ----
        [HttpGet]
        public async Task<IActionResult> ManageProfile()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return RedirectToPage("/Account/Login", new { area = "Identity" });
            }

            var roles = await _userManager.GetRolesAsync(user);
            ViewBag.Role = roles.FirstOrDefault() ?? "Client";

            // Get this client's profile details.
            var healthProfile = await _context.HealthProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == user.Id);

            // Get only this logged-in client's health records.
            var healthRecords = await _context.HealthRecords
                .AsNoTracking()
                .Where(r => r.ClientId == user.Id)
                .OrderByDescending(r => r.RecordDate)
                .ToListAsync();

            // Find the latest record containing a plausible height and weight.
            // Height may be stored in metres (1.75) or centimetres (175).
            var latestValidRecord = healthRecords.FirstOrDefault(r =>
                r.Weight > 0 &&
                r.Height > 0 &&
                (
                    (r.Height >= 0.5m && r.Height <= 2.5m) ||
                    (r.Height >= 100m && r.Height <= 250m)
                ));

            decimal weight = latestValidRecord?.Weight ?? 0m;
            decimal heightCm = 0m;
            decimal bmi = 0m;

            if (latestValidRecord != null)
            {
                heightCm = latestValidRecord.Height <= 2.5m
                    ? latestValidRecord.Height * 100m
                    : latestValidRecord.Height;

                if (heightCm > 0)
                {
                    var heightMetres = heightCm / 100m;
                    bmi = weight / (heightMetres * heightMetres);
                }
            }

            var model = new ManageProfileViewModel
            {
                FullName = user.FullName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber,
                ProfilePictureUrl = user.ProfilePictureUrl,

                // These fields come from the logged-in client's health profile.
                DateOfBirth = healthProfile?.DateOfBirth
                              ?? DateTime.Now.AddYears(-20),

                MedicalNotes = healthProfile?.MedicalNotes,

                // These fields come from that client's latest valid health record.
                Weight = weight,
                Height = heightCm,
                BMI = bmi
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
            // Persist height in metres, matching the existing database rows.
            healthProfile.Height = model.Height > 3 ? model.Height / 100 : model.Height;

            if (model.Height > 0)
            {
                // Accept either centimetres (175) or metres (1.75) from existing views.
                decimal heightInMeters = model.Height > 3 ? model.Height / 100 : model.Height;
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

    // Simple model class for Notifications
    public class NotificationItem
    {
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Time { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
    }
}