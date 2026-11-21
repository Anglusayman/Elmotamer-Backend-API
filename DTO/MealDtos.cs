using System;

namespace Elmotamer.DTOs
{
    // DTO القراءة والتحديث
    public class MealScheduleDto
    {
        public DateOnly? MealDate { get; set; }
        public TimeOnly? MealTime { get; set; }
    }
}