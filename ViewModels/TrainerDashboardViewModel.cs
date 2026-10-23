using System.Collections.Generic;
using HealthTrack.Models;

namespace HealthTrack.ViewModels
{
    public class TrainerDashboardViewModel
    {
        public int TotalClients { get; set; }

        public int PendingAppointments { get; set; }

        public int ActivePlans { get; set; }

        public int CompletedPlans { get; set; }

        public List<ApplicationUser> RecentClients { get; set; }
            = new List<ApplicationUser>();

        public List<Appointment> UpcomingAppointments { get; set; }
            = new List<Appointment>();
    }
}