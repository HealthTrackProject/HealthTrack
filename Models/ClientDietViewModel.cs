using System.Collections.Generic;

namespace HealthTrack.Models
{
    public class ClientDietViewModel
    {
        //  Meal History came from db
        public List<DietLog> MealHistory { get; set; } = new List<DietLog>();

        // mock plan for now, can be fetched from db later
        public List<MockMeal> TodayPlan { get; set; } = new List<MockMeal>();
    }

    public class MockMeal
    {
        public string Name { get; set; }
        public string Time { get; set; }
        public string Food { get; set; }
        public int Calories { get; set; }
        public string ImageUrl { get; set; }
    }
}