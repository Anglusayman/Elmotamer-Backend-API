using Elmotamer.DbContext;
using Elmotamer.DTOs;
using Elmotamer.DTOs.Elmotamer.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Elmotamer.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class IdCardController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public IdCardController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/IdCard/create-id-card
        [HttpGet("create-id-card")]
        public async Task<IActionResult> CreateIdCard()
        {
            // 1. استخراج الـ ID بأمان من الـ Token
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new { message = "المستخدم غير معرف أو الصلاحية منتهية." });
            }

            // 2. جلب بيانات الـ Attendee من جدول Attendees بناءً على الـ UserId
            var attendee = await _context.Attendees.FirstOrDefaultAsync(a => a.UserId == userId);
            if (attendee == null)
            {
                return NotFound(new { message = "عفواً، لم يتم العثور على بيانات حضور لهذا المستخدم." });
            }

            // جلب بيانات المستخدم من جدول Users لضمان استخراج أحدث صورة شخصية مرفوعة
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);

            // 3. بناء الرابط الكامل للصورة الفعليّة أونلاين وبأمان
            var request = HttpContext.Request;
            var baseUrl = $"{request.Scheme}://{request.Host}";

            string fullImageUrl = string.Empty;

            // أولاً: فحص الصورة المحدثة داخل جدول Users
            if (user != null && !string.IsNullOrEmpty(user.ImageUrl))
            {
                fullImageUrl = user.ImageUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                    ? user.ImageUrl
                    : $"{baseUrl}{(user.ImageUrl.StartsWith("/") ? "" : "/")}{user.ImageUrl}";
            }
            // ثانياً: إذا لم توجد في Users نفحص جدول Attendees
            else if (!string.IsNullOrEmpty(attendee.ImageUrl))
            {
                fullImageUrl = attendee.ImageUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                    ? attendee.ImageUrl
                    : $"{baseUrl}{(attendee.ImageUrl.StartsWith("/") ? "" : "/")}{attendee.ImageUrl}";
            }

            // 4. تعبئة الـ DTO بالبيانات الحقيقية والحقول الجديدة مع معالجة القيم الفاضية
            var response = new IdCardResponseDto
            {
                RegistrationNumber = attendee.RegistrationNumber ?? "بدون رقم تسجيل",
                FullName = attendee.FullName ?? "غير محدد",
                ServiceType = attendee.ServiceType ?? "غير محدد",
                Region = attendee.Region ?? "غير محدد",
                ChurchName = attendee.ChurchName ?? "غير محدد",
                Age = attendee.Age,
                FullImageUrl = fullImageUrl,
                IsApproved = attendee.ApprovedByAdminId != 0, // true لو تم قبول المشترك من الأدمن

                // الحقول المطلوبة (رقم الكرسي، اسم المجموعة، ومكان المجموعة)
                SeatNumber = attendee.SeatNumber,
                GroupName = string.IsNullOrWhiteSpace(attendee.GroupName) ? "غير محدد" : attendee.GroupName,
                GroupLocation = string.IsNullOrWhiteSpace(attendee.GroupLocation) ? "غير محدد" : attendee.GroupLocation
            };

            return Ok(new
            {
                message = "تم توليد بيانات الهوية الرقمية بنجاح!",
                data = response
            });
        }
    }
}