using HealthTrack.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HealthTrack.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<HealthProfile> HealthProfiles { get; set; }
        public DbSet<WorkoutPlan> WorkoutPlans { get; set; }
        public DbSet<WorkoutLog> WorkoutLogs { get; set; }
        public DbSet<DietPlan> DietPlans { get; set; }
        public DbSet<DietLog> DietLogs { get; set; }
        public DbSet<Appointment> Appointments { get; set; }
        public DbSet<HealthRecord> HealthRecords { get; set; }
        public DbSet<ProgressRecord> ProgressRecords { get; set; }
        public DbSet<Notification> Notifications { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Decimal precision
            builder.Entity<HealthProfile>()
                .Property(x => x.Weight)
                .HasPrecision(18, 2);

            builder.Entity<HealthProfile>()
                .Property(x => x.Height)
                .HasPrecision(18, 2);

            builder.Entity<HealthProfile>()
                .Property(x => x.BMI)
                .HasPrecision(18, 2);

            builder.Entity<HealthRecord>()
                .Property(x => x.Weight)
                .HasPrecision(18, 2);

            builder.Entity<HealthRecord>()
                .Property(x => x.Height)
                .HasPrecision(18, 2);

            builder.Entity<HealthRecord>()
                .Property(x => x.BMI)
                .HasPrecision(18, 2);

            builder.Entity<ProgressRecord>()
                .Property(x => x.Weight)
                .HasPrecision(18, 2);

            builder.Entity<ProgressRecord>()
                .Property(x => x.BMI)
                .HasPrecision(18, 2);


            // HealthProfile → User
            builder.Entity<HealthProfile>()
                .HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);


            // WorkoutPlan → Trainer
            builder.Entity<WorkoutPlan>()
                .HasOne(x => x.Trainer)
                .WithMany()
                .HasForeignKey(x => x.TrainerId)
                .OnDelete(DeleteBehavior.Restrict);

            // WorkoutPlan → Client
            builder.Entity<WorkoutPlan>()
                .HasOne(x => x.Client)
                .WithMany()
                .HasForeignKey(x => x.ClientId)
                .OnDelete(DeleteBehavior.Restrict);


            // WorkoutLog → Client
            builder.Entity<WorkoutLog>()
                .HasOne(x => x.Client)
                .WithMany()
                .HasForeignKey(x => x.ClientId)
                .OnDelete(DeleteBehavior.Restrict);

            // WorkoutLog → WorkoutPlan
            builder.Entity<WorkoutLog>()
                .HasOne(x => x.WorkoutPlan)
                .WithMany()
                .HasForeignKey(x => x.WorkoutPlanId)
                .OnDelete(DeleteBehavior.Restrict);


            // DietPlan → Trainer
            builder.Entity<DietPlan>()
                .HasOne(x => x.Trainer)
                .WithMany()
                .HasForeignKey(x => x.TrainerId)
                .OnDelete(DeleteBehavior.Restrict);

            // DietPlan → Client
            builder.Entity<DietPlan>()
                .HasOne(x => x.Client)
                .WithMany()
                .HasForeignKey(x => x.ClientId)
                .OnDelete(DeleteBehavior.Restrict);


            // DietLog → Client
            builder.Entity<DietLog>()
                .HasOne(x => x.Client)
                .WithMany()
                .HasForeignKey(x => x.ClientId)
                .OnDelete(DeleteBehavior.Restrict);

            // DietLog → DietPlan
            builder.Entity<DietLog>()
                .HasOne(x => x.DietPlan)
                .WithMany()
                .HasForeignKey(x => x.DietPlanId)
                .OnDelete(DeleteBehavior.Restrict);


            // Appointment → Client
            builder.Entity<Appointment>()
                .HasOne(x => x.Client)
                .WithMany()
                .HasForeignKey(x => x.ClientId)
                .OnDelete(DeleteBehavior.Restrict);

            // Appointment → Trainer
            builder.Entity<Appointment>()
                .HasOne(x => x.Trainer)
                .WithMany()
                .HasForeignKey(x => x.TrainerId)
                .OnDelete(DeleteBehavior.Restrict);


            // HealthRecord → Client
            builder.Entity<HealthRecord>()
                .HasOne(x => x.Client)
                .WithMany()
                .HasForeignKey(x => x.ClientId)
                .OnDelete(DeleteBehavior.Restrict);


            // ProgressRecord → Client
            builder.Entity<ProgressRecord>()
                .HasOne(x => x.Client)
                .WithMany()
                .HasForeignKey(x => x.ClientId)
                .OnDelete(DeleteBehavior.Restrict);


            // Notification → User
            builder.Entity<Notification>()
                .HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}