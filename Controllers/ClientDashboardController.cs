using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity; // Added for Identity and User Management
using HealthTrack.Models;            // Added to access ApplicationUser
using System.Threading.Tasks;        // Added for async tasks
using System.Collections.Generic;    // Added for List
using Microsoft.AspNetCore.Authorization; // Added for [Authorize] attribute

namespace HealthTrack.Controllers
{
    [Authorize] // Ensures only logged-in users can access these pages
    public class ClientDashboardController : Controller
    {
        // Required to fetch the logged-in user's details
        private readonly UserManager<ApplicationUser> _userManager;

        // Constructor to inject UserManager
        public ClientDashboardController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        // ---- Your existing actions (unchanged) ----
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
        // --------------------------------------------

        // ---- Newly added actions ----

        // Action for Notifications Page
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

        // Action for My Profile Page
        public async Task<IActionResult> ManageProfile()
        {
            // Fetch the logged-in user
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToPage("/Account/Login", new { area = "Identity" });
            }

            // Get the user's role to display on the profile
            var roles = await _userManager.GetRolesAsync(user);
            ViewBag.Role = roles.Count > 0 ? roles[0] : "Client";

            return View(user);
        }
    }

    // Simple model class for Notifications (must be outside the controller)
    public class NotificationItem
    {
        public string Title { get; set; }
        public string Message { get; set; }
        public string Time { get; set; }
        public string Icon { get; set; }
        public string Color { get; set; }
    }
}