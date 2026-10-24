namespace HealthTrack.ViewModels;

public class AdminDashboardViewModel
{
    public int TotalUsers { get; set; }
    public int TotalClients { get; set; }
    public int TotalTrainers { get; set; }
    public int TotalAdmins { get; set; }
    public int TotalAppointments { get; set; }

    public int ClientPercentage =>
        TotalUsers == 0 ? 0 : (int)Math.Round(TotalClients * 100.0 / TotalUsers);

    public int TrainerPercentage =>
        TotalUsers == 0 ? 0 : (int)Math.Round(TotalTrainers * 100.0 / TotalUsers);

    public int AdminPercentage =>
        TotalUsers == 0 ? 0 : (int)Math.Round(TotalAdmins * 100.0 / TotalUsers);

    public List<AdminActivityViewModel> RecentActivities { get; set; } = new();
}

public class AdminActivityViewModel
{
    public string Title { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string Type { get; set; } = "info";
}