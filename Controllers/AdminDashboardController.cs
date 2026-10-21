using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace HealthTrack.Controllers
{
    [Authorize(Roles = "Admin")] // Only accessible by Admins
    public class AdminDashboardController : Controller
    {
        public IActionResult Index() { return View(); }
        public IActionResult ManageUser() { return View(); }
        public IActionResult ManageClient() { return View(); }
        public IActionResult ManageTrainers() { return View(); }
        public IActionResult Appointments() { return View(); }
        public IActionResult Reports() { return View(); }
        public IActionResult SystemSettings() { return View(); }
        public IActionResult Notifications() { return View(); }
        public IActionResult MyProfile() { return View(); }
    }
}