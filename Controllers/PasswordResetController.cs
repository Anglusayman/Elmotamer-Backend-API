using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Elmotamer.DTOs;
using Elmotamer.DbContext;
using System.Threading.Tasks;
using FirebaseAdmin.Auth; // مكتبة Firebase للتحقق الحقيقي من التوكن
using BCrypt.Net;         // 👈 تم إضافة مكتبة BCrypt للتشفير الحقيقي
using System;

namespace Elmotamer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PasswordResetController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public PasswordResetController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Endpoint الموحدة لتحديث الباسورد بعد نجاح التحقق في الفلاتر عبر Firebase
        [HttpPost("update-password")]
        public async Task<IActionResult> UpdatePassword([FromBody] ResetPasswordDto dto)
        {
            // 1. التحقق من مدخلات الطلب وأن كل الحقول ممتلئة
            if (dto == null || string.IsNullOrEmpty(dto.PhoneNumber) || string.IsNullOrEmpty(dto.NewPassword) || string.IsNullOrEmpty(dto.FirebaseToken))
            {
                return BadRequest(new { message = "جميع الحقول مطلوبة (رقم الهاتف، كلمة المرور الجديدة، والتوكن)." });
            }

            // 2. 🔒 الخطوة الأمنية الحقيقية: التحقق من التوكن القادم من Firebase والربط برقم الهاتف
            bool isTokenValid = await VerifyFirebaseTokenAsync(dto.FirebaseToken, dto.PhoneNumber);
            if (!isTokenValid)
            {
                return BadRequest(new { message = "فشل التحقق من الهوية، هذا الطلب غير مصرح به، منتهي الصلاحية، أو لا يطابق رقم الهاتف!" });
            }

            // 3. البحث عن المستخدم برقم الهاتف في قاعدة البيانات
            var user = await _context.Users.FirstOrDefaultAsync(u => u.PhoneNumber == dto.PhoneNumber);
            if (user == null)
            {
                return NotFound(new { message = "عفواً، رقم الهاتف هذا غير مسجل لدينا في النظام." });
            }

            // 4. 🔒 تشفير كلمة المرور الجديدة باستخدام BCrypt لضمان أمان الحساب وتوافقه مع الـ Login
            user.PasswordHash = HashPassword(dto.NewPassword);

            // 5. حفظ التعديلات نهائياً في قاعدة البيانات
            await _context.SaveChangesAsync();

            return Ok(new { message = "تم تحديث كلمة المرور بنجاح. يمكنك الآن الانتقال لتسجيل الدخول." });
        }

        // دالة مساعدة حقيقية تفحص التوكن وتربطه برقم الهاتف المستهدف
        private async Task<bool> VerifyFirebaseTokenAsync(string token, string phoneNumber)
        {
            try
            {
                // 1. فحص التوكن وفك تشفيره والتأكد من توقيعه الرقمي عبر سيرفرات Firebase
                FirebaseToken decodedToken = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(token);

                // 2. استخراج رقم الهاتف الذي تم التحقق منه فعلياً داخل Firebase
                if (decodedToken.Claims.TryGetValue("phone_number", out var firebasePhoneObj) && firebasePhoneObj != null)
                {
                    string firebasePhoneNumber = firebasePhoneObj.ToString();

                    // 3. تنظيف الأرقام لمقارنتها بشكل مرن (للتغلب على فروق رمز الدولة مثل +20 أو 0020)
                    string cleanFirebasePhone = firebasePhoneNumber.Replace("+", "").Trim();
                    string cleanUserPhone = phoneNumber.Replace("+", "").Trim();

                    // التأكد من أن التوكن يخص نفس رقم الهاتف المطلوبة إعادة تعيين باسورده
                    if (cleanFirebasePhone.EndsWith(cleanUserPhone) || cleanUserPhone.EndsWith(cleanFirebasePhone))
                    {
                        return true;
                    }
                }

                return false;
            }
            catch (Exception)
            {
                // في حال كان التوكن مزوراً، منتهي الصلاحية، أو حدث خطأ في الاتصال بسيرفرات Firebase
                return false;
            }
        }

        // دالة مساعدة لتشفير كلمة المرور باستخدام مكتبة BCrypt
        private string HashPassword(string password)
        {
            // 🔐 تشفير كلمة المرور بـ Salt عشوائي تلقائي مدمج من المكتبة
            return BCrypt.Net.BCrypt.HashPassword(password);
        }
    }
}