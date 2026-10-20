using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace HealthTrack.Controllers
{
    [Authorize(Roles = "Trainer")] // Only accessible by Trainers
    public class TrainerDashboardController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        // අනිත් Actions ටික මෙතනට එකතු කරන්න (MyClient, WorkoutPlans, etc.)
        public IActionResult MyClient() { return View(); }
        public IActionResult WorkoutPlans() { return View(); }
        public IActionResult DietPlans() { return View(); }
        public IActionResult Appointments() { return View(); }
        public IActionResult ClientProgress() { return View(); }
        public IActionResult Feedback() { return View(); }
        public IActionResult Notifications() { return View(); }
        public IActionResult MyProfile() { return View(); }
    }
}