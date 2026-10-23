using HealthTrack.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HealthTrack.ViewModels
{
    public class TrainerFeedbackViewModel
    {
        public string ClientId { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;

        public List<SelectListItem> Clients { get; set; } = new();

        public List<ClientFeedback> RecentFeedback { get; set; } = new();
    }
}