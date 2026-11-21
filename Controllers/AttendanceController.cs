using Elmotamer.DbContext;
using Elmotamer.DTOs;
using Elmotamer.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Elmotamer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AttendanceController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AttendanceController(ApplicationDbContext context)
        {
            _context = context;
        }

        [Authorize(Roles = "Admin,SuperAdmin")]
        [HttpPost("scan-and-admit/{registrationNumber}")]
        public async Task<IActionResult> ScanAndAdmitAttendee(string registrationNumber)
        {
            // 1. جلب الـ ID الخاص بالأدمن الحالي من الـ JWT Token بأمان
            var adminIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(adminIdClaim) || !int.TryParse(adminIdClaim, out int adminId))
            {
                return Unauthorized(new { message = "فشلت عملية التحقق من هوية المسؤول الصلاحية غير صالحة." });
            }

            // 2. البحث عن اليوزر الأساسي في جدول الـ Users برقم التسجيل الفريد
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.RegistrationNumber == registrationNumber);

            if (user == null)
            {
                return NotFound(new { message = "عفواً، كود التسجيل هذا غير صحيح أو غير مسجل في النظام!" });
            }

            // 3. منع التكرار: التأكد أن المشترك لم يسجل دخوله مسبقاً في جدول الـ Attendees
            var isAlreadyAdmitted = await _context.Attendees
                .AnyAsync(a => a.RegistrationNumber == registrationNumber);

            if (isAlreadyAdmitted)
            {
                return BadRequest(new { message = $"تنبيه: المشترك ({user.FullName}) مسجل حضور ومقبول بالفعل مسبقاً!" });
            }

            // 4. حساب رقم الكرسي التلقائي والتأكد من عدم اكتمال العدد (16 إلى 346)
            int lastSeatNumber = await _context.Attendees
                .MaxAsync(a => (int?)a.SeatNumber) ?? 15;

            int nextSeatNumber = lastSeatNumber + 1;

            if (nextSeatNumber > 346)
            {
                return BadRequest(new { message = "عفواً، تم اكتمال العدد الأقصى للمقاعد المتاحة (346 مشترك)!" });
            }

            // 5. تحديد اسم المجموعة تلقائياً بناءً على رقم الكرسي
            string groupName = GetGroupNameBySeatNumber(nextSeatNumber);

            // 6. بناء الـ Model ونقل البيانات بما فيها الـ ImageUrl والمقعد والمجموعة
            var newAttendee = new Attendee
            {
                UserId = user.UserId,
                FullName = user.FullName,
                Age = user.Age,
                ChurchName = user.ChurchName,
                ServiceType = user.ServiceType,
                Region = user.Region,
                RegistrationNumber = user.RegistrationNumber,
                ImageUrl = user.ImageUrl,
                ApprovedByAdminId = adminId,
                ApprovedAt = DateTime.UtcNow,
                SeatNumber = nextSeatNumber,
                GroupName = groupName,
                GroupLocation = null // افتراضياً null
            };

            // 7. حفظ الحضور في قاعدة البيانات
            _context.Attendees.Add(newAttendee);
            await _context.SaveChangesAsync();

            // 8. بناء رابط الصورة الكامل (Absolute URL) ليظهر بوضوح في الفلاتر
            var request = HttpContext.Request;
            var baseUrl = $"{request.Scheme}://{request.Host}";
            var fullImageUrl = string.IsNullOrEmpty(newAttendee.ImageUrl)
                ? $"{baseUrl}/uploads/default-avatar.png"
                : $"{baseUrl}{newAttendee.ImageUrl}";

            // 9. تجهيز الـ DTO المخصص للرد
            var responseDto = new AttendeeResponseDto
            {
                AttendeeId = newAttendee.AttendeeId,
                FullName = newAttendee.FullName,
                ChurchName = newAttendee.ChurchName,
                ServiceType = newAttendee.ServiceType,
                RegistrationNumber = newAttendee.RegistrationNumber,
                ImageUrl = fullImageUrl,
                ApprovedAt = newAttendee.ApprovedAt,
                SeatNumber = newAttendee.SeatNumber,
                GroupName = newAttendee.GroupName,
                GroupLocation = newAttendee.GroupLocation
            };

            return Ok(responseDto);
        }

        // --- Endpoint تحديث مكان المجموعة (متاحة للأدمن والسوبر أدمن) ---
        [Authorize(Roles = "Admin,SuperAdmin")]
        [HttpPut("update-group-location")]
        public async Task<IActionResult> UpdateGroupLocation([FromBody] UpdateGroupLocationDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.GroupName) || string.IsNullOrWhiteSpace(dto.Location))
            {
                return BadRequest(new { message = "يرجى تحديد اسم المجموعة والمكان بشكل صحيح." });
            }

            var groupAttendees = await _context.Attendees
                .Where(a => a.GroupName == dto.GroupName)
                .ToListAsync();

            if (!groupAttendees.Any())
            {
                return NotFound(new { message = $"لم يتم العثور على أي مشتركين مسجلين في مجموعة ({dto.GroupName}) حتى الآن!" });
            }

            foreach (var attendee in groupAttendees)
            {
                attendee.GroupLocation = dto.Location;
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = $"تم تحديث مكان مجموعة ({dto.GroupName}) إلى ({dto.Location}) بنجاح لعدد {groupAttendees.Count} مشترك." });
        }

        // --- Endpoint تقرير الحضور الخاص بالسوبر أدمن فقط ---
        [Authorize(Roles = "SuperAdmin")]
        [HttpGet("superadmin-report")]
        public async Task<IActionResult> GetAttendeesReportForSuperAdmin()
        {
            // جلب العدد الإجمالي وقائمة المشتركين مع ربط رقم الهاتف من جدول Users عبر الـ UserId
            var attendeesQuery = await _context.Attendees
                .Join(_context.Users,
                    attendee => attendee.UserId,
                    user => user.UserId,
                    (attendee, user) => new AttendeeSummaryDto
                    {
                        FullName = attendee.FullName,
                        RegistrationNumber = attendee.RegistrationNumber,
                        PhoneNumber = user.PhoneNumber
                    })
                .ToListAsync();

            var reportDto = new SuperAdminAttendeesReportDto
            {
                TotalAttendeesCount = attendeesQuery.Count,
                Attendees = attendeesQuery
            };

            return Ok(reportDto);
        }

        // دالة مخصصة لتحديد اسم المجموعة بناءً على رقم الكرسي
        private static string GetGroupNameBySeatNumber(int seatNumber)
        {
            return seatNumber switch
            {
                >= 16 and <= 70 => "محبة",
                >= 71 and <= 125 => "التعفف",
                >= 126 and <= 180 => "سلام",
                >= 181 and <= 235 => "طول أناة",
                >= 236 and <= 290 => "فرح",
                >= 291 and <= 346 => "وداعة",
                _ => "غير محدد"
            };
        }
    }
}