using System;
using System.ComponentModel.DataAnnotations;

namespace Elmotamer.Models
{
    public class MealSchedule
    {
        [Key]
        public int Id { get; set; } = 1;

        public DateOnly? MealDate { get; set; } // 📅 تاريخ فقط
        public TimeOnly? MealTime { get; set; } // ⏰ وقت فقط
    }
}