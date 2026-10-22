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

        // ---- Dashboard Index ----
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            var healthProfile = await _context.HealthProfiles.FirstOrDefaultAsync(h => h.UserId == user.Id);

            var startOfWeek = DateTime.Now.AddDays(-7);
            var weeklyLogs = await _context.WorkoutLogs
                .Where(w => w.ClientId == user.Id && w.WorkoutDate >= startOfWeek)
                .ToListAsync();

            var upcomingAppointments = await _context.Appointments
                .Include(a => a.Trainer)
                .Where(a => a.ClientId == user.Id && a.AppointmentDate >= DateTime.Now && a.Status == "Approved")
                .OrderBy(a => a.AppointmentDate)
                .Take(3)
                .ToListAsync();

            var weightHistory = await _context.HealthRecords
                .Where(h => h.ClientId == user.Id)
                .OrderByDescending(h => h.RecordDate)
                .Take(6)
                .ToListAsync();

            weightHistory.Reverse();

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

                // මෙතන තමයි අර වැරැද්ද තිබ්බේ. දැන් හරි!
                CaloriesChartData = GetWeeklyCaloriesChartData(weeklyLogs)
            };

            if (!model.WeightChartData.Any())
            {
                model.WeightChartData = new List<ChartDataPoint>
                {
                    new ChartDataPoint { Label = "Jan", Value = 70 },
                    new ChartDataPoint { Label = "Feb", Value = 69 }
                };
            }

            return View(model);
        }

        private List<ChartDataPoint> GetWeeklyCaloriesChartData(List<WorkoutLog> logs)
        {
            var chartData = new List<ChartDataPoint>();
            for (int i = 6; i >= 0; i--)
            {
                var date = DateTime.Now.AddDays(-i).Date;
                var dayName = date.ToString("ddd"); // Mon, Tue, Wed...

                // Get total calories for this specific day
                var calories = logs.Where(w => w.WorkoutDate.Date == date).Sum(w => w.CaloriesBurned);

                chartData.Add(new ChartDataPoint
                {
                    Label = dayName,
                    Value = calories
                });
            }
            return chartData;
        }

        // ---- Health Records Actions ----

        // 1. Show Health Records Page (GET)
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
                CurrentWeight = profile?.Weight ?? 0,
                CurrentHeight = profile?.Height ?? 0,
                CurrentBMI = profile?.BMI ?? 0,
                LastHeartRate = latestRecord?.HeartRate ?? 0,
                Records = records
            };

            return View(model);
        }

        // 2. Add New Record (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddHealthRecord(decimal weight, decimal height, int caloriesBurned, DateTime recordDate, int? heartRate, string? bloodPressure)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            decimal bmi = 0;
            if (height > 0)
            {
                decimal heightInMeters = height / 100;
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

        // ---- Other Pages ----

        // 1. GET:when Load the Workout Page 
        public async Task<IActionResult> Workout()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            // user fetch Workout History from db
            var workoutHistory = await _context.WorkoutLogs
                .Where(w => w.ClientId == user.Id)
                .OrderByDescending(w => w.WorkoutDate)
                .ToListAsync();

            // Current Plan (mock up data for now)
            var currentPlan = new List<WorkoutDay>
    {
        new WorkoutDay { Day = "Monday", MuscleGroup = "Chest & Arms", Exercises = "Push-ups, Bench Press, Bicep Curls" },
        new WorkoutDay { Day = "Tuesday", MuscleGroup = "Legs", Exercises = "Squats, Lunges, Leg Press" },
        new WorkoutDay { Day = "Wednesday", MuscleGroup = "Cardio", Exercises = "Running, Cycling, HIIT" },
        new WorkoutDay { Day = "Thursday", MuscleGroup = "Back & Shoulders", Exercises = "Deadlifts, Shoulder Press" },
        new WorkoutDay { Day = "Friday", MuscleGroup = "Full Body", Exercises = "Circuit Training" }
    };

            var model = new WorkoutViewModel
            {
                WorkoutHistory = workoutHistory,
                CurrentPlan = currentPlan
            };

            return View(model);
        }

        // 2. POST: data that comes from the form
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LogWorkout(string workoutName, string workoutGroup, string workoutExercises, int workoutDuration, int? workoutCalories, DateTime workoutDate)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            // WorkoutPlanId is compulsory. If user cannot have a plan, then create it.
            var plan = await _context.WorkoutPlans.FirstOrDefaultAsync(p => p.ClientId == user.Id);

            if (plan == null)
            {
                // if user doesn't have a plan, create a dummy plan for them
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

            // create a new Workout Log and save it
            var newLog = new WorkoutLog
            {
                ClientId = user.Id,
                WorkoutPlanId = plan.WorkoutPlanId,
                ExerciseName = $"{workoutName} ({workoutGroup}) - {workoutExercises}", // Combine all text for DB
                DurationMinutes = workoutDuration,
                CaloriesBurned = workoutCalories ?? 0,
                WorkoutDate = workoutDate
            };

            _context.WorkoutLogs.Add(newLog);
            await _context.SaveChangesAsync();

            return RedirectToAction("Workout");
        }
        // 1. GET: when load the diet page
        public async Task<IActionResult> Diet()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            // Fetch Meal History from Database
            var mealHistory = await _context.DietLogs
                .Where(d => d.ClientId == user.Id)
                .OrderByDescending(d => d.MealDate)
                .ToListAsync();

            // Today's Plan (Mock Data)
            var todayPlan = new List<MockMeal>
    {
        new MockMeal { Name = "Breakfast", Time = "7:00 AM", Food = "Oatmeal, Banana, Almonds", Calories = 350, ImageUrl = "https://images.unsplash.com/photo-1517673132405-a56a62b18caf?w=200&h=200&fit=crop" },
        new MockMeal { Name = "Lunch", Time = "12:00 PM", Food = "Grilled Chicken, Brown Rice, Salad", Calories = 550, ImageUrl = "https://images.unsplash.com/photo-1546069901-ba9599a7e63c?w=200&h=200&fit=crop" },
        new MockMeal { Name = "Dinner", Time = "7:00 PM", Food = "Fish, Vegetables, Quinoa", Calories = 450, ImageUrl = "https://images.unsplash.com/photo-1467003909585-2f8a72700288?w=200&h=200&fit=crop" },
        new MockMeal { Name = "Snacks", Time = "4:00 PM", Food = "Greek Yogurt, Nuts", Calories = 200, ImageUrl = "https://images.unsplash.com/photo-1488477181946-6428a0291777?w=200&h=200&fit=crop" }
    };

            var model = new DietViewModel
            {
                MealHistory = mealHistory,
                TodayPlan = todayPlan
            };

            return View(model);
        }

        // 2. POST: Submit data via Log Meal Form 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LogMeal(string mealType, string foodDescription, int calories, DateTime mealDate)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            // DietPlanId is compulsory for DietLog 
            // If the user doesn't have a diet plan, create a new one.
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

    // Fixed warnings by adding = string.Empty;
    public class NotificationItem
    {
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Time { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
    }
}