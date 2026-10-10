
using Microsoft.AspNetCore.Mvc;

namespace HealthTrack.Controllers
{
    public class ClientDashboardController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult HealthRecords()
        {
            return View();
        }
    }
}
