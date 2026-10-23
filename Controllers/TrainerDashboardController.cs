using HealthTrack.Data;
using HealthTrack.Models;
using HealthTrack.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace HealthTrack.Controllers
{
    [Authorize(Roles = "Trainer")]
    public class TrainerDashboardController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public TrainerDashboardController(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        // --------------------------------------------------
        // 1. TRAINER DASHBOARD
        // --------------------------------------------------
        public async Task<IActionResult> Index()
        {
            var trainer = await _userManager.GetUserAsync(User);

            if (trainer == null)
            {
                return Challenge();
            }

            var today = DateTime.Today;
            var now = DateTime.Now;

            // Find clients connected to this trainer
            // through workout plans, diet plans and appointments.
            var workoutClientIds = await _context.WorkoutPlans
                .Where(p => p.TrainerId == trainer.Id)
                .Select(p => p.ClientId)
                .Distinct()
                .ToListAsync();

            var dietClientIds = await _context.DietPlans
                .Where(p => p.TrainerId == trainer.Id)
                .Select(p => p.ClientId)
                .Distinct()
                .ToListAsync();

            var appointmentClientIds = await _context.Appointments
                .Where(a => a.TrainerId == trainer.Id)
                .Select(a => a.ClientId)
                .Distinct()
                .ToListAsync();

            var clientIds = workoutClientIds
                .Concat(dietClientIds)
                .Concat(appointmentClientIds)
                .Where(id => !string.IsNullOrEmpty(id))
                .Distinct()
                .ToList();

            // Total clients
            var totalClients = clientIds.Count;

            // Recent clients
            var recentClients = await _context.Users
                .Where(u => clientIds.Contains(u.Id))
                .OrderBy(u => u.FullName)
                .Take(5)
                .ToListAsync();

            // Pending appointments
            var pendingAppointments = await _context.Appointments
                .CountAsync(a =>
                    a.TrainerId == trainer.Id &&
                    a.Status == "Pending");

            // Active workout plans
            var activeWorkoutPlans = await _context.WorkoutPlans
                .CountAsync(p =>
                    p.TrainerId == trainer.Id &&
                    p.StartDate <= today &&
                    p.EndDate >= today);

            // Active diet plans
            var activeDietPlans = await _context.DietPlans
                .CountAsync(p =>
                    p.TrainerId == trainer.Id &&
                    p.StartDate <= today &&
                    p.EndDate >= today);

            var activePlans = activeWorkoutPlans + activeDietPlans;

            // Plans whose end date has passed
            var completedWorkoutPlans = await _context.WorkoutPlans
                .CountAsync(p =>
                    p.TrainerId == trainer.Id &&
                    p.EndDate < today);

            var completedDietPlans = await _context.DietPlans
                .CountAsync(p =>
                    p.TrainerId == trainer.Id &&
                    p.EndDate < today);

            var completedPlans =
                completedWorkoutPlans + completedDietPlans;

            // Upcoming appointments
            var upcomingAppointments = await _context.Appointments
                .Include(a => a.Client)
                .Where(a =>
                    a.TrainerId == trainer.Id &&
                    a.AppointmentDate >= now &&
                    a.Status != "Cancelled" &&
                    a.Status != "Completed")
                .OrderBy(a => a.AppointmentDate)
                .Take(5)
                .ToListAsync();

            // Build dashboard ViewModel
            var model = new TrainerDashboardViewModel
            {
                TotalClients = totalClients,
                PendingAppointments = pendingAppointments,
                ActivePlans = activePlans,
                CompletedPlans = completedPlans,
                RecentClients = recentClients,
                UpcomingAppointments = upcomingAppointments
            };

            ViewData["Title"] = "Trainer Dashboard";
            ViewData["Heading"] = "Welcome, Trainer";
            ViewData["ActivePage"] = "Dashboard";

            return View(model);
        }

        // --------------------------------------------------
        // 2. OTHER TRAINER PAGES
        // --------------------------------------------------
        public async Task<IActionResult> MyClient()
        {
            var trainer = await _userManager.GetUserAsync(User);

            if (trainer == null)
            {
                return Challenge();
            }

            // 1. Get client IDs from workout plans
            var workoutClientIds = await _context.WorkoutPlans
                .Where(p => p.TrainerId == trainer.Id)
                .Select(p => p.ClientId)
                .Distinct()
                .ToListAsync();

            // 2. Get client IDs from diet plans
            var dietClientIds = await _context.DietPlans
                .Where(p => p.TrainerId == trainer.Id)
                .Select(p => p.ClientId)
                .Distinct()
                .ToListAsync();

            // 3. Get client IDs from appointments
            var appointmentClientIds = await _context.Appointments
                .Where(a => a.TrainerId == trainer.Id)
                .Select(a => a.ClientId)
                .Distinct()
                .ToListAsync();

            // 4. Combine all client IDs without duplicates
            var clientIds = workoutClientIds
                .Concat(dietClientIds)
                .Concat(appointmentClientIds)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct()
                .ToList();

            // 5. Fetch client Identity records
            var users = await _context.Users
                .Where(u => clientIds.Contains(u.Id))
                .OrderBy(u => u.FullName)
                .ToListAsync();

            var clients = new List<TrainerClientViewModel>();

            // 6. Fetch each client's latest progress and health records
            foreach (var user in users)
            {
                var progress = await _context.ProgressRecords
                    .Where(p => p.ClientId == user.Id)
                    .OrderByDescending(p => p.RecordDate)
                    .FirstOrDefaultAsync();

                var health = await _context.HealthRecords
                    .Where(h => h.ClientId == user.Id)
                    .OrderByDescending(h => h.RecordDate)
                    .FirstOrDefaultAsync();

                clients.Add(new TrainerClientViewModel
                {
                    Id = user.Id,

                    FullName = string.IsNullOrWhiteSpace(user.FullName)
                        ? (user.Email ?? "Client")
                        : user.FullName,

                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    ProfilePictureUrl = user.ProfilePictureUrl,

                    WorkoutsCompleted = progress?.WorkoutsCompleted ?? 0,

                    Weight = health?.Weight,
                    BMI = health?.BMI,

                    HealthSummary = health == null
                        ? "No health records available."
                        : $"Latest weight: {health.Weight:F2} kg; BMI: {health.BMI:F2}"
                });
            }

            // 7. Set page information
            ViewData["Title"] = "My Clients";
            ViewData["Heading"] = "Manage Clients";
            ViewData["ActivePage"] = "MyClients";

            // 8. Send data to the view
            return View(clients);

        }
        // Verify that this client is connected to the logged-in trainer.
        private async Task<bool> CanAccessClientAsync(
            string trainerId,
            string clientId)
        {
            if (string.IsNullOrWhiteSpace(clientId))
                return false;

            var hasWorkout = await _context.WorkoutPlans.AnyAsync(p =>
                p.TrainerId == trainerId && p.ClientId == clientId);

            var hasDiet = await _context.DietPlans.AnyAsync(p =>
                p.TrainerId == trainerId && p.ClientId == clientId);

            var hasAppointment = await _context.Appointments.AnyAsync(a =>
                a.TrainerId == trainerId && a.ClientId == clientId);

            return hasWorkout || hasDiet || hasAppointment;
        }

        // Save a workout assignment.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignWorkout(
            string clientId,
            string plan,
            string? notes,
            DateTime startDate,
            DateTime endDate)
        {
            var trainer = await _userManager.GetUserAsync(User);

            if (trainer == null)
                return Unauthorized(new { success = false, message = "Please sign in again." });

            if (!await CanAccessClientAsync(trainer.Id, clientId))
                return BadRequest(new { success = false, message = "This client is not assigned to you." });

            if (string.IsNullOrWhiteSpace(plan) || plan.Length > 150)
                return BadRequest(new { success = false, message = "Enter a valid workout plan name." });

            if (startDate == default || endDate == default || endDate.Date < startDate.Date)
                return BadRequest(new { success = false, message = "Please select valid start and end dates." });

            var clientExists = await _context.Users.AnyAsync(u => u.Id == clientId);

            if (!clientExists)
                return BadRequest(new { success = false, message = "Client account was not found." });

            var workoutPlan = new WorkoutPlan
            {
                TrainerId = trainer.Id,
                ClientId = clientId,
                PlanName = plan.Trim(),
                Description = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
                StartDate = startDate.Date,
                EndDate = endDate.Date
            };

            _context.WorkoutPlans.Add(workoutPlan);
            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                message = $"Workout plan '{workoutPlan.PlanName}' was assigned successfully."
            });
        }

        // Save a diet assignment.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignDietPlan(
            string clientId,
            string plan,
            string? notes,
            DateTime startDate,
            DateTime endDate)
        {
            var trainer = await _userManager.GetUserAsync(User);

            if (trainer == null)
                return Unauthorized(new { success = false, message = "Please sign in again." });

            if (!await CanAccessClientAsync(trainer.Id, clientId))
                return BadRequest(new { success = false, message = "This client is not assigned to you." });

            if (string.IsNullOrWhiteSpace(plan) || plan.Length > 150)
                return BadRequest(new { success = false, message = "Enter a valid diet plan name." });

            if (startDate == default || endDate == default || endDate.Date < startDate.Date)
                return BadRequest(new { success = false, message = "Please select valid start and end dates." });

            var clientExists = await _context.Users.AnyAsync(u => u.Id == clientId);

            if (!clientExists)
                return BadRequest(new { success = false, message = "Client account was not found." });

            var dietPlan = new DietPlan
            {
                TrainerId = trainer.Id,
                ClientId = clientId,
                PlanName = plan.Trim(),
                Description = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
                StartDate = startDate.Date,
                EndDate = endDate.Date
            };

            _context.DietPlans.Add(dietPlan);
            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                message = $"Diet plan '{dietPlan.PlanName}' was assigned successfully."
            });
        }
        [HttpGet]
        public async Task<IActionResult> WorkoutPlans()
        {
            var trainer = await _userManager.GetUserAsync(User);

            if (trainer == null)
                return Challenge();

            var clients = await _userManager.GetUsersInRoleAsync("Client");

            var model = new TrainerWorkoutPlanViewModel
            {
                StartDate = DateTime.Today,
                EndDate = DateTime.Today.AddMonths(1),

                Clients = clients
                    .OrderBy(c => c.FullName)
                    .Select(c => new SelectListItem
                    {
                        Value = c.Id,
                        Text = string.IsNullOrWhiteSpace(c.FullName)
                            ? c.Email ?? "Client"
                            : c.FullName
                    })
                    .ToList()
            };

            ViewData["Title"] = "Workout Plans";
            ViewData["Heading"] = "Workout Plans";
            ViewData["ActivePage"] = "WorkoutPlans";

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> WorkoutPlans(
            TrainerWorkoutPlanViewModel model)
        {
            var trainer = await _userManager.GetUserAsync(User);

            if (trainer == null)
                return Challenge();

            var clients = await _userManager.GetUsersInRoleAsync("Client");

            // Refill the dropdown if validation fails.
            model.Clients = clients
                .OrderBy(c => c.FullName)
                .Select(c => new SelectListItem
                {
                    Value = c.Id,
                    Text = string.IsNullOrWhiteSpace(c.FullName)
                        ? c.Email ?? "Client"
                        : c.FullName
                })
                .ToList();

            if (model.EndDate.Date < model.StartDate.Date)
            {
                ModelState.AddModelError(
                    nameof(model.EndDate),
                    "End date must be on or after the start date.");
            }

            var selectedClient = clients.FirstOrDefault(
                c => c.Id == model.ClientId);

            if (selectedClient == null)
            {
                ModelState.AddModelError(
                    nameof(model.ClientId),
                    "Please select a valid client.");
            }

            if (model.Days == null || model.Days.Count == 0)
            {
                ModelState.AddModelError(
                    nameof(model.Days),
                    "Please add at least one workout day.");
            }
            else if (model.Days.Count > 7)
            {
                ModelState.AddModelError(
                    nameof(model.Days),
                    "You can add a maximum of seven workout days.");
            }

            if (!ModelState.IsValid)
            {
                ViewData["Title"] = "Workout Plans";
                ViewData["Heading"] = "Workout Plans";
                ViewData["ActivePage"] = "WorkoutPlans";

                return View(model);
            }

            var workoutPlan = new WorkoutPlan
            {
                TrainerId = trainer.Id,
                ClientId = model.ClientId,
                PlanName = model.PlanName.Trim(),
                Description = model.Description?.Trim(),
                StartDate = model.StartDate.Date,
                EndDate = model.EndDate.Date
            };

            _context.WorkoutPlans.Add(workoutPlan);
            await _context.SaveChangesAsync();

            // Save the individual workout days.
            var workoutDays = model.Days.Select((day, index) =>
                new WorkoutPlanDay
                {
                    WorkoutPlanId = workoutPlan.WorkoutPlanId,
                    DayNumber = index + 1,
                    DayName = string.IsNullOrWhiteSpace(day.DayName)
                        ? $"Day {index + 1}"
                        : day.DayName.Trim(),
                    MuscleGroup = day.MuscleGroup?.Trim(),
                    Exercises = day.Exercises?.Trim()
                }).ToList();

            _context.WorkoutPlanDays.AddRange(workoutDays);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Workout plan saved successfully!";

            return RedirectToAction(nameof(WorkoutPlans));
        }

        [HttpGet]
        public async Task<IActionResult> DietPlans()
        {
            var trainer = await _userManager.GetUserAsync(User);

            if (trainer == null)
                return Challenge();

            var clients = await _userManager.GetUsersInRoleAsync("Client");

            var model = new TrainerDietPlanViewModel
            {
                StartDate = DateTime.Today,
                EndDate = DateTime.Today.AddDays(30),

                Clients = clients
                    .OrderBy(c => c.FullName)
                    .Select(c => new SelectListItem
                    {
                        Value = c.Id,
                        Text = string.IsNullOrWhiteSpace(c.FullName)
                            ? (c.Email ?? "Client")
                            : c.FullName
                    })
                    .ToList()
            };

            ViewData["Title"] = "Diet Plans";
            ViewData["Heading"] = "Diet Plans";
            ViewData["ActivePage"] = "DietPlans";

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DietPlans(
            TrainerDietPlanViewModel model)
        {
            var trainer = await _userManager.GetUserAsync(User);

            if (trainer == null)
                return Challenge();

            // Reload the clients dropdown if validation fails.
            var clients = await _userManager.GetUsersInRoleAsync("Client");

            model.Clients = clients
                .OrderBy(c => c.FullName)
                .Select(c => new SelectListItem
                {
                    Value = c.Id,
                    Text = string.IsNullOrWhiteSpace(c.FullName)
                        ? (c.Email ?? "Client")
                        : c.FullName
                })
                .ToList();

            var selectedClient = clients.FirstOrDefault(
                c => c.Id == model.ClientId);

            if (selectedClient == null)
            {
                ModelState.AddModelError(
                    nameof(model.ClientId),
                    "Please select a valid client.");
            }

            if (model.StartDate == default ||
                model.EndDate == default ||
                model.EndDate.Date < model.StartDate.Date)
            {
                ModelState.AddModelError(
                    nameof(model.EndDate),
                    "End date must be on or after the start date.");
            }

            if (model.Meals == null ||
                model.Meals.Count == 0 ||
                model.Meals.Count > 20)
            {
                ModelState.AddModelError(
                    nameof(model.Meals),
                    "Please add between 1 and 20 meals.");
            }
            else
            {
                foreach (var meal in model.Meals)
                {
                    if (string.IsNullOrWhiteSpace(meal.MealType))
                    {
                        ModelState.AddModelError(
                            nameof(model.Meals),
                            "Every meal must have a name.");
                    }

                    if (string.IsNullOrWhiteSpace(meal.FoodDescription))
                    {
                        ModelState.AddModelError(
                            nameof(model.Meals),
                            "Enter food details for every meal.");
                    }

                    if (meal.Calories < 0 || meal.Calories > 10000)
                    {
                        ModelState.AddModelError(
                            nameof(model.Meals),
                            "Calories must be between 0 and 10,000.");
                    }
                }
            }

            if (!ModelState.IsValid)
            {
                ViewData["Title"] = "Diet Plans";
                ViewData["Heading"] = "Diet Plans";
                ViewData["ActivePage"] = "DietPlans";

                return View(model);
            }

            var dietPlan = new DietPlan
            {
                TrainerId = trainer.Id,
                ClientId = selectedClient!.Id,
                PlanName = model.PlanName.Trim(),

                Description = string.IsNullOrWhiteSpace(model.Description)
                    ? null
                    : model.Description.Trim(),

                StartDate = model.StartDate.Date,
                EndDate = model.EndDate.Date,

                Meals = model.Meals.Select(meal => new DietPlanMeal
                {
                    MealType = meal.MealType.Trim(),
                    MealTime = meal.MealTime,
                    FoodDescription = meal.FoodDescription.Trim(),
                    Calories = meal.Calories,
                    ImageUrl = string.IsNullOrWhiteSpace(meal.ImageUrl)
                        ? null
                        : meal.ImageUrl.Trim()
                }).ToList()
            };

            _context.DietPlans.Add(dietPlan);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Diet plan and meals saved successfully!";

            return RedirectToAction(nameof(DietPlans));
        }


        [Authorize(Roles = "Trainer")]
        public async Task<IActionResult> Appointments()
        {
            var trainer = await _userManager.GetUserAsync(User);

            if (trainer == null)
            {
                return Challenge();
            }

            var appointments = await _context.Appointments
                .AsNoTracking()
                .Where(a => a.TrainerId == trainer.Id)
                .Include(a => a.Client)
                .OrderByDescending(a => a.AppointmentDate)
                .ToListAsync();

            ViewData["Title"] = "Appointments";
            ViewData["Heading"] = "Manage Appointments";
            ViewData["ActivePage"] = "Appointments";

            return View(appointments);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Trainer")]
        public async Task<IActionResult> UpdateAppointmentStatus(
    int appointmentId,
    string status)
        {
            var trainer = await _userManager.GetUserAsync(User);

            if (trainer == null)
            {
                return Challenge();
            }

            var allowedStatuses = new[]
            {
        "Pending",
        "Confirmed",
        "Completed",
        "Cancelled"
    };

            if (string.IsNullOrWhiteSpace(status) ||
                !allowedStatuses.Contains(status, StringComparer.OrdinalIgnoreCase))
            {
                TempData["Error"] = "Invalid appointment status.";
                return RedirectToAction(nameof(Appointments));
            }

            var appointment = await _context.Appointments
                .FirstOrDefaultAsync(a =>
                    a.AppointmentId == appointmentId &&
                    a.TrainerId == trainer.Id);

            if (appointment == null)
            {
                return NotFound();
            }

            appointment.Status = allowedStatuses.First(
                s => s.Equals(status, StringComparison.OrdinalIgnoreCase));

            await _context.SaveChangesAsync();

            TempData["Success"] = "Appointment status updated successfully.";

            return RedirectToAction(nameof(Appointments));
        }
        public IActionResult ClientProgress()
        {
            return View();
        }

        public IActionResult Feedback()
        {
            return View();
        }

        // --------------------------------------------------
        // 3. TRAINER NOTIFICATIONS
        // --------------------------------------------------
        // These notifications are sample data.
        // This action is not yet connected to the database.
        [Authorize(Roles = "Trainer")]
        public async Task<IActionResult> Notifications()
        {
            var trainer = await _userManager.GetUserAsync(User);

            if (trainer == null)
            {
                return Challenge();
            }

            var notifications = await _context.Notifications
                .AsNoTracking()
                .Where(n => n.UserId == trainer.Id)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            ViewData["Title"] = "Notifications";
            ViewData["Heading"] = "Notifications";
            ViewData["ActivePage"] = "Notifications";

            return View(notifications);
        }
        // --------------------------------------------------
        // 4. SHOW TRAINER PROFILE
        // --------------------------------------------------
        [HttpGet]
        public async Task<IActionResult> MyProfile()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Challenge();
            }

            var roles = await _userManager.GetRolesAsync(user);

            ViewBag.Role = roles.Count > 0
                ? roles[0]
                : "Trainer";

            var healthProfile = await _context.HealthProfiles
                .FirstOrDefaultAsync(p => p.UserId == user.Id);

            if (healthProfile == null)
            {
                healthProfile = new HealthProfile
                {
                    UserId = user.Id,
                    Weight = 0,
                    Height = 0,
                    DateOfBirth = DateTime.Now.AddYears(-25)
                };
            }

            var model = new ManageProfileViewModel
            {
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                ProfilePictureUrl = user.ProfilePictureUrl,

                DateOfBirth = healthProfile.DateOfBirth == default
                    ? DateTime.Now.AddYears(-25)
                    : healthProfile.DateOfBirth,

                Weight = healthProfile.Weight,
                Height = healthProfile.Height,
                BMI = healthProfile.BMI,
                MedicalNotes = healthProfile.MedicalNotes
            };

            return View(model);
        }

        // --------------------------------------------------
        // 5. UPDATE TRAINER PROFILE
        // --------------------------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(
            ManageProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] =
                    "Please check the information you entered.";

                return RedirectToAction(nameof(MyProfile));
            }

            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Challenge();
            }

            user.FullName = model.FullName;
            user.PhoneNumber = model.PhoneNumber;

            var identityResult = await _userManager.UpdateAsync(user);

            if (!identityResult.Succeeded)
            {
                TempData["Error"] =
                    string.Join(" ", identityResult.Errors
                        .Select(e => e.Description));

                return RedirectToAction(nameof(MyProfile));
            }

            var healthProfile = await _context.HealthProfiles
                .FirstOrDefaultAsync(p => p.UserId == user.Id);

            if (healthProfile == null)
            {
                healthProfile = new HealthProfile
                {
                    UserId = user.Id
                };

                _context.HealthProfiles.Add(healthProfile);
            }

            healthProfile.DateOfBirth = model.DateOfBirth;
            healthProfile.Weight = model.Weight;
            healthProfile.Height = model.Height;
            healthProfile.MedicalNotes = model.MedicalNotes;

            if (model.Height > 0)
            {
                decimal heightInMeters = model.Height / 100m;

                healthProfile.BMI =
                    model.Weight / (heightInMeters * heightInMeters);
            }
            else
            {
                healthProfile.BMI = 0;
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "Profile updated successfully.";

            return RedirectToAction(nameof(MyProfile));
        }

        // --------------------------------------------------
        // 6. UPLOAD TRAINER PROFILE PICTURE
        // --------------------------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadProfilePicture(
            IFormFile profilePic)
        {
            if (profilePic == null || profilePic.Length == 0)
            {
                TempData["Error"] = "Please select an image.";

                return RedirectToAction(nameof(MyProfile));
            }

            // Limit the uploaded file to 5 MB.
            const long maxFileSize = 5 * 1024 * 1024;

            if (profilePic.Length > maxFileSize)
            {
                TempData["Error"] =
                    "The image size must not exceed 5 MB.";

                return RedirectToAction(nameof(MyProfile));
            }

            var allowedExtensions = new[]
            {
                ".jpg",
                ".jpeg",
                ".png"
            };

            var extension = Path
                .GetExtension(profilePic.FileName)
                .ToLowerInvariant();

            if (!allowedExtensions.Contains(extension))
            {
                TempData["Error"] =
                    "Only JPG, JPEG and PNG images are allowed.";

                return RedirectToAction(nameof(MyProfile));
            }

            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Challenge();
            }

            var uploadPath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                "uploads",
                "profiles");

            Directory.CreateDirectory(uploadPath);

            var fileName = Guid.NewGuid().ToString("N") + extension;
            var filePath = Path.Combine(uploadPath, fileName);

            try
            {
                using (var stream = new FileStream(
                    filePath,
                    FileMode.CreateNew,
                    FileAccess.Write))
                {
                    await profilePic.CopyToAsync(stream);
                }

                user.ProfilePictureUrl = fileName;

                var result = await _userManager.UpdateAsync(user);

                if (!result.Succeeded)
                {
                    System.IO.File.Delete(filePath);

                    TempData["Error"] =
                        "The profile picture could not be saved.";

                    return RedirectToAction(nameof(MyProfile));
                }

                TempData["Success"] =
                    "Profile picture uploaded successfully.";
            }
            catch (IOException)
            {
                TempData["Error"] =
                    "An error occurred while uploading the image.";
            }

            return RedirectToAction(nameof(MyProfile));
        }
    }
}