using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Elmotamer.DbContext;
using Elmotamer.DTOs;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Elmotamer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminAuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public AdminAuthController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // إندبوينت تسجيل أدمن جديد أو سوبر أدمن وتشفير الباسورد بـ BCrypt تلقائياً
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterAdminDto dto)
        {
            // 1. التحقق من عدم تكرار البريد الإلكتروني
            var isEmailExists = await _context.Admins.AnyAsync(a => a.Email == dto.Email);
            if (isEmailExists)
            {
                return BadRequest(new { message = "هذا البريد الإلكتروني مسجل بالفعل لأدمن آخر." });
            }

            // 2. تشفير كلمة المرور باستخدام مكتبة BCrypt بشكل سليم 100%
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

            // 3. إنشاء كائن الأدمن الجديد وتعبئة البيانات (مع الـ Role المختار)
            var newAdmin = new Models.Admin
            {
                Email = dto.Email,
                PasswordHash = passwordHash,
                FullName = dto.FullName,
                PhoneNumber = dto.PhoneNumber,
                Age = dto.Age,
                Role = dto.Role, // هنا بتختار تكتب "Admin" أو "SuperAdmin" في الـ Body
                CreatedAt = DateTime.UtcNow
            };

            // 4. الحفظ في قاعدة البيانات
            _context.Admins.Add(newAdmin);
            await _context.SaveChangesAsync();

            return Ok(new { message = "تم تسجيل الحساب بنجاح!" });
        }

        // تسجيل دخول الأدمن فقط (من جدول Admins)
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] AdminLoginDto dto)
        {
            // 1. البحث عن الأدمن بالإيميل داخل جدول الـ Admins المنفصل
            var admin = await _context.Admins.FirstOrDefaultAsync(a => a.Email == dto.Email);

            // 2. التحقق من وجوده ومطابقة كلمة المرور المشفرة بـ BCrypt
            if (admin == null || !BCrypt.Net.BCrypt.Verify(dto.Password, admin.PasswordHash))
                return Unauthorized(new { message = "بيانات الدخول الخاصة بالأدمن غير صحيحة." });

            // 3. توليد التوكن الخاص بالأدمن (بصلاحية 3 سنوات ويحتوي على الـ Role الفعلي)
            var token = GenerateAdminJwtToken(admin);

            return Ok(new
            {
                token = token,
                role = admin.Role,
                fullName = admin.FullName,
                message = "تم تسجيل دخول الأدمن بنجاح!"
            });
        }

        // دالة توليد التوكن للأدمن وضبط الـ Claims والـ Role بدقة
        private string GenerateAdminJwtToken(Models.Admin admin)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"]);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[] {
                    new Claim(ClaimTypes.NameIdentifier, admin.AdminId.ToString()), // حفظ الـ AdminId
                    new Claim(ClaimTypes.Email, admin.Email),
                    new Claim(ClaimTypes.Role, admin.Role) // قراءة الـ Role من جدولك ("Admin" أو "SuperAdmin") لقفل الحماية
                }),
                // صلاحية التوكن 3 سنوات كاملة
                Expires = DateTime.UtcNow.AddDays(365 * 3),

                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
                Issuer = _configuration["Jwt:Issuer"],
                Audience = _configuration["Jwt:Audience"]
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}