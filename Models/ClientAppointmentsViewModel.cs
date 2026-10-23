using System;
using System.Collections.Generic;

namespace HealthTrack.Models
{
    public class ClientAppointmentsViewModel
    {
        public List<Appointment> UpcomingAppointments { get; set; } = new List<Appointment>();
        public List<Appointment> PastAppointments { get; set; } = new List<Appointment>();

        // Trainer list that need to show in the form dropdown
        public List<ApplicationUser> AvailableTrainers { get; set; } = new List<ApplicationUser>();
    }
}