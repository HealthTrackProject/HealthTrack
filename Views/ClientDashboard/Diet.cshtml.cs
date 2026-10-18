using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;

namespace HealthTrack.Views.ClientDashboard
{
    public class DietModel : PageModel
    {
        public List<Meal> TodayPlan { get; set; } = new List<Meal>();

        public void OnGet()
        {
            // UI එක පෙන්වන්න දැනට Dummy Data ටිකක් හදමු
            TodayPlan = new List<Meal>
            {
                new Meal { Name = "Breakfast", Time = "7:00 AM", Food = "Oatmeal, Banana, Almonds", Calories = "350 cal", ImageUrl = "https://images.unsplash.com/photo-1517673132405-a56a62b18caf?w=100&h=100&fit=crop" },
                new Meal { Name = "Lunch", Time = "12:00 PM", Food = "Grilled Chicken, Brown Rice, Salad", Calories = "550 cal", ImageUrl = "https://images.unsplash.com/photo-1546069901-ba9599a7e63c?w=100&h=100&fit=crop" },
                new Meal { Name = "Dinner", Time = "7:00 PM", Food = "Fish, Vegetables, Quinoa", Calories = "450 cal", ImageUrl = "https://images.unsplash.com/photo-1467003909585-2f8a72700288?w=100&h=100&fit=crop" },
                new Meal { Name = "Snacks", Time = "4:00 PM", Food = "Greek Yogurt, Nuts", Calories = "200 cal", ImageUrl = "https://images.unsplash.com/photo-1488477181946-6428a0291777?w=100&h=100&fit=crop" }
            };
        }

        public class Meal
        {
            public string Name { get; set; }
            public string Time { get; set; }
            public string Food { get; set; }
            public string Calories { get; set; }
            public string ImageUrl { get; set; }
        }
    }
}
