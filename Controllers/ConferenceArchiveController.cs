using Elmotamer.DbContext;
using Elmotamer.DTOs;
using Elmotamer.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Elmotamer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "SuperAdmin")]
    public class ConferenceArchiveController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ConferenceArchiveController> _logger;

        public ConferenceArchiveController(
            ApplicationDbContext context,
            ILogger<ConferenceArchiveController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpPost("archive-and-reset")]
        public async Task<IActionResult> ArchiveAndResetConference([FromBody] ArchiveAndResetConferenceDto request)
        {
            // 1. التأكيد الأمني
            if (!request.ConfirmReset)
            {
                return BadRequest(new
                {
                    Message = "لم يتم تأكيد عملية المسح والأرشفة. يرجى إرسال ConfirmReset = true لتأكيد العملية."
                });
            }

            // 2. بداية الـ Transaction
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // جلب المشتركين الحاليين
                var currentAttendees = await _context.Attendees.ToListAsync();

                if (!currentAttendees.Any())
                {
                    return BadRequest(new { Message = "جدول المشتركين فارغ بالفعل، لا توجد بيانات لنقلها للأرشيف." });
                }

                // 2.5 جلب أرقام الهواتف الخاصة بالمستخدمين من جدول الـ Users دفعة واحدة لرفع كفاءة الأداء
                var userIds = currentAttendees.Select(a => a.UserId).Distinct().ToList();
                var userPhones = await _context.Users
                    .Where(u => userIds.Contains(u.UserId))
                    .ToDictionaryAsync(u => u.UserId, u => u.PhoneNumber);

                // 3. تجهيز بيانات الأرشيف وقراءة رقم الهاتف من قاموس الـ Users
                var historyRecords = currentAttendees.Select(a => new AttendeesHistory
                {
                    UserId = a.UserId,
                    FullName = string.IsNullOrWhiteSpace(a.FullName) ? "غير محدد" : a.FullName,
                    PhoneNumber = userPhones.TryGetValue(a.UserId, out var phone) && !string.IsNullOrWhiteSpace(phone)
                                  ? phone
                                  : "غير محدد",
                    RegistrationNumber = string.IsNullOrWhiteSpace(a.RegistrationNumber) ? a.AttendeeId.ToString() : a.RegistrationNumber,
                    ConferenceDate = request.ConferenceDate,
                    ArchivedAt = DateTime.UtcNow
                }).ToList();

                // 4. الحفظ في جدول الـ History
                await _context.AttendeesHistories.AddRangeAsync(historyRecords);
                await _context.SaveChangesAsync();

                // 5. مسح جدول المشتركين
                _context.Attendees.RemoveRange(currentAttendees);
                await _context.SaveChangesAsync();

                // 6. تصفير الـ Identity Counter
                await _context.Database.ExecuteSqlRawAsync("DBCC CHECKIDENT ('Attendees', RESEED, 0);");

                // 7. Commit الـ Transaction
                await transaction.CommitAsync();

                _logger.LogInformation("تم أرشفة {Count} مشترك لمؤتمر بتاريخ {Date} وتم إعادة ضبط الجدول بنجاح.",
                    historyRecords.Count, request.ConferenceDate.ToShortDateString());

                return Ok(new
                {
                    Success = true,
                    Message = $"تم نقل {historyRecords.Count} مشترك لجدول الأرشيف وتصفير جدول المشتركين بنجاح للمؤتمر الجديد.",
                    ArchivedCount = historyRecords.Count,
                    ConferenceDate = request.ConferenceDate
                });
            }
            catch (Exception ex)
            {
                // Rollback في حالة حدوث أي خطأ
                await transaction.RollbackAsync();

                _logger.LogError(ex, "حدث خطأ أثناء أرشفة وتصفير جدول المشتركين.");

                return StatusCode(500, new
                {
                    Message = "حدث خطأ أثناء عملية الأرشفة، تم إلغاء العملية ولم تُمسح أي بيانات.",
                    Details = ex.Message
                });
            }
        }
    }
}