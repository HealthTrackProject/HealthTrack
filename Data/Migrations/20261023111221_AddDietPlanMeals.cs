using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HealthTrack.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDietPlanMeals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DietPlanMeals",
                columns: table => new
                {
                    DietPlanMealId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DietPlanId = table.Column<int>(type: "int", nullable: false),
                    MealType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MealTime = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    FoodDescription = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Calories = table.Column<int>(type: "int", nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DietPlanMeals", x => x.DietPlanMealId);
                    table.ForeignKey(
                        name: "FK_DietPlanMeals_DietPlans_DietPlanId",
                        column: x => x.DietPlanId,
                        principalTable: "DietPlans",
                        principalColumn: "DietPlanId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DietPlanMeals_DietPlanId",
                table: "DietPlanMeals",
                column: "DietPlanId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DietPlanMeals");
        }
    }
}
