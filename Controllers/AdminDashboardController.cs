using ClosedXML.Excel;
using DocumentFormat.OpenXml.InkML;
using HealthTrack.Data;
using HealthTrack.Models;
using HealthTrack.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
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
        // GET: /AdminDashboard/Appointments
        public async Task<IActionResult> Appointments()
        {
            var appointments = await _context.Appointments
                .Include(a => a.Client)
                .Include(a => a.Trainer)
                .OrderByDescending(a => a.AppointmentDate)
                .ToListAsync();

            // Fix legacy database values so the UI displays correctly
            foreach (var appt in appointments)
            {
                if (string.IsNullOrEmpty(appt.Status))
                {
                    appt.Status = "Pending";
                }
                else if (appt.Status.Equals("Approved", StringComparison.OrdinalIgnoreCase))
                {
                    appt.Status = "Confirmed"; // Map old "Approved" to "Confirmed"
                }
            }

            var clients = await _userManager.GetUsersInRoleAsync("Client");
            var trainers = await _userManager.GetUsersInRoleAsync("Trainer");

            ViewBag.Clients = clients;
            ViewBag.Trainers = trainers;

            return View(appointments);
        }

        // POST: /AdminDashboard/CreateAppointment
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateAppointment(string clientId, string trainerId, DateTime appointmentDate, string reason)
        {
            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(trainerId))
            {
                TempData["ToastMessage"] = "Please select both a client and a trainer.";
                TempData["ToastType"] = "error";
                return RedirectToAction(nameof(Appointments));
            }

            var newAppointment = new Appointment
            {
                ClientId = clientId,
                TrainerId = trainerId,
                AppointmentDate = appointmentDate,
                Reason = reason ?? "Admin booked appointment",
                Status = "Pending" // Defaults to Pending
            };

            _context.Appointments.Add(newAppointment);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Appointments));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateAppointmentStatus(int id, string status)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment != null)
            {
                // Translate "Approved" to "Confirmed" to match your database
                appointment.Status = (status == "Approved") ? "Confirmed" : status;

                await _context.SaveChangesAsync();

                TempData["ToastMessage"] = $"Appointment marked as {appointment.Status}.";
                TempData["ToastType"] = "success";
            }
            return RedirectToAction(nameof(Appointments));
        }

        // GET: /AdminDashboard/Reports
        public async Task<IActionResult> Reports(int months = 6)
        {
            if (months != 3 && months != 6 && months != 12) months = 6;
            ViewBag.SelectedMonths = months;

            var startDate = DateTime.Now.AddMonths(-months);

            // 1. Fetch Data from Database
            var users = await _userManager.Users.ToListAsync();
            var appointments = await _context.Appointments
                .Where(a => a.AppointmentDate >= startDate)
                .ToListAsync();
            var workoutPlans = await _context.WorkoutPlans.ToListAsync();

            // 2. Calculate Stats
            var model = new AdminDashboardViewModel
            {
                TotalUsers = users.Count,
                TotalAppointments = appointments.Count,
                TotalWorkoutPlans = workoutPlans.Count,
                TotalClients = (await _userManager.GetUsersInRoleAsync("Client")).Count
            };

            // 3. Prepare Chart Data (Last N Months)
            var labels = new List<string>();
            var userGrowthData = new List<int>();
            var appointmentData = new List<int>();

            for (int i = months - 1; i >= 0; i--)
            {
                var monthDate = DateTime.Now.AddMonths(-i);
                labels.Add(monthDate.ToString("MMM"));

                // --- FIXED USER GROWTH LOGIC ---
                // This calculates the real count, but safely ignores the broken "0001" dates from your screenshot
                int monthlyUsers = users.Count(u =>
                    u.CreatedAt != null &&
                    u.CreatedAt.Year > 2000 && // Ignores 1/1/0001
                    u.CreatedAt.Month == monthDate.Month &&
                    u.CreatedAt.Year == monthDate.Year);

                userGrowthData.Add(monthlyUsers);

                // Appointment logic remains the same
                var monthAppts = appointments.Count(a => a.AppointmentDate.Month == monthDate.Month && a.AppointmentDate.Year == monthDate.Year);
                appointmentData.Add(monthAppts);
            }

            ViewBag.ChartLabels = labels;
            ViewBag.UserGrowthData = userGrowthData;
            ViewBag.AppointmentData = appointmentData;

            return View(model);
        }        // GET: /AdminDashboard/ExportExcel
        public async Task<IActionResult> ExportExcel(int months = 6)
    {
        var startDate = DateTime.Now.AddMonths(-months);
        var appointments = await _context.Appointments
            .Include(a => a.Client)
            .Include(a => a.Trainer)
            .Where(a => a.AppointmentDate >= startDate)
            .OrderByDescending(a => a.AppointmentDate)
            .ToListAsync();

        using (var workbook = new XLWorkbook())
        {
            var worksheet = workbook.Worksheets.Add("Appointments Report");
            var currentRow = 1;

            // Headers
            worksheet.Cell(currentRow, 1).Value = "Date";
            worksheet.Cell(currentRow, 2).Value = "Client";
            worksheet.Cell(currentRow, 3).Value = "Trainer";
            worksheet.Cell(currentRow, 4).Value = "Status";
            worksheet.Cell(currentRow, 5).Value = "Reason";

            // Style Headers
            var headerRange = worksheet.Range(currentRow, 1, currentRow, 5);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

            // Data
            foreach (var appt in appointments)
            {
                currentRow++;
                worksheet.Cell(currentRow, 1).Value = appt.AppointmentDate.ToString("yyyy-MM-dd HH:mm");
                worksheet.Cell(currentRow, 2).Value = appt.Client?.FullName ?? "Unknown";
                worksheet.Cell(currentRow, 3).Value = appt.Trainer?.FullName ?? "Unknown";
                worksheet.Cell(currentRow, 4).Value = appt.Status;
                worksheet.Cell(currentRow, 5).Value = appt.Reason;
            }

            worksheet.Columns().AdjustToContents();

            using (var stream = new MemoryStream())
            {
                workbook.SaveAs(stream);
                var content = stream.ToArray();
                return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Report_Last{months}Months.xlsx");
            }
        }
    }

    // GET: /AdminDashboard/ExportPdf
    public async Task<IActionResult> ExportPdf(int months = 6)
    {
        var startDate = DateTime.Now.AddMonths(-months);
        var appointments = await _context.Appointments
            .Include(a => a.Client)
            .Include(a => a.Trainer)
            .Where(a => a.AppointmentDate >= startDate)
            .OrderByDescending(a => a.AppointmentDate)
            .ToListAsync();

        // FIXED: Generating a real PDF using QuestPDF instead of a .txt file
        var pdfDocument = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(12));

                // Header
                page.Header().Text($"Appointments Report (Last {months} Months)")
                    .SemiBold().FontSize(20).FontColor(Colors.Blue.Darken2);

                // Table
                page.Content().PaddingVertical(10).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(90); // Date
                        columns.RelativeColumn();   // Client
                        columns.RelativeColumn();   // Trainer
                        columns.ConstantColumn(80); // Status
                    });

                    table.Header(header =>
                    {
                        header.Cell().Text("Date").SemiBold();
                        header.Cell().Text("Client").SemiBold();
                        header.Cell().Text("Trainer").SemiBold();
                        header.Cell().Text("Status").SemiBold();
                    });

                    foreach (var appt in appointments)
                    {
                        table.Cell().Text(appt.AppointmentDate.ToString("dd MMM yyyy"));
                        table.Cell().Text(appt.Client?.FullName ?? "Unknown");
                        table.Cell().Text(appt.Trainer?.FullName ?? "Unknown");
                        table.Cell().Text(appt.Status);
                    }
                });

                // Footer
                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Page ");
                    x.CurrentPageNumber();
                });
            });
        });

        var pdfBytes = pdfDocument.GeneratePdf();
        return File(pdfBytes, "application/pdf", $"Report_Last{months}Months.pdf");
    }
        // GET: /AdminDashboard/SystemSettings
        public async Task<IActionResult> SystemSettings()
        {
            // Get the settings. If the table is empty, create the default row.
            var settings = await _context.SystemSettings.FirstOrDefaultAsync();

            if (settings == null)
            {
                settings = new SystemSetting();
                _context.SystemSettings.Add(settings);
                await _context.SaveChangesAsync();
            }

            return View(settings);
        }

        // POST: /AdminDashboard/SystemSettings
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SystemSettings(SystemSetting model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var settings = await _context.SystemSettings.FirstOrDefaultAsync();
            if (settings == null)
            {
                settings = new SystemSetting();
                _context.SystemSettings.Add(settings);
            }

            // Update the values
            settings.SystemName = model.SystemName;
            settings.SupportEmail = model.SupportEmail;
            settings.DefaultTimezone = model.DefaultTimezone;

            settings.TwoFactorAuth = model.TwoFactorAuth;
            settings.MaintenanceMode = model.MaintenanceMode;
            settings.ForcePasswordReset = model.ForcePasswordReset;

            settings.EmailNotifications = model.EmailNotifications;
            settings.SmsAlerts = model.SmsAlerts;

            settings.MaxUploadSize = model.MaxUploadSize;
            settings.AllowUserRegistration = model.AllowUserRegistration;

            await _context.SaveChangesAsync();

            TempData["ToastMessage"] = "System settings updated successfully!";
            TempData["ToastType"] = "success";

            return RedirectToAction(nameof(SystemSettings));
        }        // GET: /AdminDashboard/Notifications
        public async Task<IActionResult> Notifications()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            // Fetch real notifications for this Admin
            var notifications = await _context.Notifications
                .Where(n => n.UserId == user.Id)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            return View(notifications);
        }

        // POST: /AdminDashboard/MarkAllNotificationsRead
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllNotificationsRead()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            var unreadNotifications = await _context.Notifications
                .Where(n => n.UserId == user.Id && !n.IsRead)
                .ToListAsync();

            foreach (var notif in unreadNotifications)
            {
                notif.IsRead = true;
            }

            await _context.SaveChangesAsync();
            TempData["ToastMessage"] = "All notifications marked as read.";
            TempData["ToastType"] = "success";

            return RedirectToAction(nameof(Notifications));
        }

        // POST: /AdminDashboard/MarkNotificationRead
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkNotificationRead(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            var notif = await _context.Notifications.FirstOrDefaultAsync(n => n.NotificationId == id && n.UserId == user.Id);
            if (notif != null && !notif.IsRead)
            {
                notif.IsRead = true;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Notifications));
        }
        // ---- Profile Actions ----

        // 1. Show Admin Profile
        [HttpGet]
        public async Task<IActionResult> MyProfile()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Challenge();

            var roles = await _userManager.GetRolesAsync(user);

            ViewBag.Role = roles.Contains("Admin")
                ? "System Adminstrator"
                : roles.FirstOrDefault() ?? "Admin";

            var model = new TrainerManageProfileViewModel
            {
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber,
                ProfilePictureUrl = user.ProfilePictureUrl
            };

            return View(model);
        }

        // --------------------------------------------------
        // 5. UPDATE TRAINER PROFILE
        // --------------------------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(
     TrainerManageProfileViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Challenge();

            if (string.IsNullOrWhiteSpace(model.FullName))
            {
                TempData["Error"] = "Full name is required.";
                return RedirectToAction(nameof(MyProfile));
            }

            user.FullName = model.FullName.Trim();
            user.PhoneNumber = string.IsNullOrWhiteSpace(model.PhoneNumber)
                ? null
                : model.PhoneNumber.Trim();

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                TempData["Error"] = string.Join(
                    " ",
                    result.Errors.Select(e => e.Description));

                return RedirectToAction(nameof(MyProfile));
            }

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