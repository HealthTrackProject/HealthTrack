using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HealthTrack.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSystemSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SystemSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SystemName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SupportEmail = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DefaultTimezone = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TwoFactorAuth = table.Column<bool>(type: "bit", nullable: false),
                    MaintenanceMode = table.Column<bool>(type: "bit", nullable: false),
                    ForcePasswordReset = table.Column<bool>(type: "bit", nullable: false),
                    EmailNotifications = table.Column<bool>(type: "bit", nullable: false),
                    SmsAlerts = table.Column<bool>(type: "bit", nullable: false),
                    MaxUploadSize = table.Column<int>(type: "int", nullable: false),
                    AllowUserRegistration = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemSettings", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SystemSettings");
        }
    }
}
