
using Elmotamer.DbContext;
using Elmotamer.DTOs;
using Elmotamer.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace Elmotamer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class MealsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public MealsController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// قراءة ميعاد الوجبة (مفتوح للمعتمدين)
        /// GET: api/Meals/schedule
        /// </summary>
        [HttpGet("schedule")]
        public async Task<IActionResult> GetSchedule()
        {
            var schedule = await _context.MealSchedules.FirstOrDefaultAsync(m => m.Id == 1);

            return Ok(new MealScheduleDto
            {
                MealDate = schedule?.MealDate,
                MealTime = schedule?.MealTime
            });
        }

        /// <summary>
        /// تحديث ميعاد الوجبة (للأدمن فقط)
        /// PUT: api/Meals/update-schedule
        /// </summary>
        [HttpPut("update-schedule")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> UpdateSchedule([FromBody] MealScheduleDto dto)
        {
            var schedule = await _context.MealSchedules.FirstOrDefaultAsync(m => m.Id == 1);

            if (schedule == null)
            {
                schedule = new MealSchedule { Id = 1 };
                _context.MealSchedules.Add(schedule);
            }

            schedule.MealDate = dto.MealDate;
            schedule.MealTime = dto.MealTime;

            await _context.SaveChangesAsync();

            return Ok(new { message = "تم تحديث ميعاد الوجبة بنجاح!" });
        }
    }
}