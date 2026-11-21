using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Elmotamer.DbContext;
using Elmotamer.DTOs;
using Elmotamer.Models; // تم التأكد من استدعاء موديل الـ User
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authorization;
using FirebaseAdmin.Auth; // تم إضافة مكتبة الفايربيز للتحقق الأمني من الـ OTP

namespace Elmotamer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _env; // إضافة الـ Environment للوصول لمجلد الـ wwwroot بأمان

        public AuthController(ApplicationDbContext context, IConfiguration configuration, IWebHostEnvironment env)
        {
            _context = context;
            _configuration = configuration;
            _env = env;
        }

        // 1. تسجيل مستخدم جديد مع التحقق الأمني من الـ Firebase OTP Token
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            // أ. التحقق من البريد الإلكتروني أولاً في قاعدة البيانات
            if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
                return BadRequest(new { message = "هذا البريد الإلكتروني مسجل بالفعل." });

            // ب. التحقق الأمني من صحة توكن الفايربيز القادم من الفلاتر قبل لمس الداتابيز
            try
            {
                // فك تشفير وتأكيد الـ idToken الصادر بعد نجاح الـ OTP بالفرونت إند
                FirebaseToken decodedToken = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(dto.FirebaseToken);

                // (اختياري) يمكنك فك رقم الهاتف المسجل بالتوكن ومقارنته بالـ dto إذا أردت زيادة التأمين:
                // string fbPhoneNumber = decodedToken.Claims.ContainsKey("phone_number") ? decodedToken.Claims["phone_number"].ToString() : "";
            }
            catch (Exception)
            {
                // في حالة التلاعب بالتوكن أو انتهاء صلاحيته يتم رفض العملية فوراً وحماية السيرفر
                return BadRequest(new { message = "فشل التحقق من رمز الـ OTP، الـ Token غير صالح أو منتهي الصلاحية." });
            }

            // ج. تشفير كلمة المرور وتوليد رقم المسابقة / الحساب الفريد
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(dto.Password);
            string regNumber = "CONF-" + Guid.NewGuid().ToString().Substring(0, 6).ToUpper();

            // د. إنشاء كيان المستخدم وتفعيل حالة الهاتف مباشرة نظراً لتخطي خطوة الفايربيز بنجاح
            var user = new User
            {
                Email = dto.Email,
                PhoneNumber = dto.PhoneNumber,
                PasswordHash = hashedPassword,
                FullName = dto.FullName,
                Age = dto.Age,
                ChurchName = dto.ChurchName,
                ServiceType = dto.ServiceType,
                Region = dto.Region,
                RegistrationNumber = regNumber,
                Role = "User",
                IsPhoneVerified = true, // تم تأكيده بنجاح عبر السيرفر
                CreatedAt = DateTime.UtcNow,
                ImageUrl = null // افتراضياً الحساب الجديد ملوش صورة لحد ما يرفعها
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var token = GenerateJwtToken(user);

            return Ok(new
            {
                message = "تم تسجيل الحساب بنجاح وتفعيل الهاتف!",
                registrationNumber = regNumber,
                token = token
            });
        }

        // 2. تسجيل الدخول وتوليد التوكن
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);

            if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                return Unauthorized(new { message = "البريد الإلكتروني أو كلمة المرور غير صحيحة." });

            var token = GenerateJwtToken(user);

            return Ok(new
            {
                token = token,
                message = "تم تسجيل الدخول بنجاح"
            });
        }

        // 3. جلب بيانات الملف الشخصي (شاملة رابط الصورة)
        [Authorize]
        [HttpGet("get-profile")]
        public async Task<IActionResult> GetProfile()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new { message = "المستخدم غير معرف أو الصلاحية غير صالحة." });
            }

            var userProfile = await _context.Users
                .Where(u => u.UserId == userId)
                .Select(u => new UserProfileDto
                {
                    UserId = u.UserId,
                    Email = u.Email,
                    PhoneNumber = u.PhoneNumber,
                    FullName = u.FullName,
                    Age = u.Age,
                    ChurchName = u.ChurchName,
                    ServiceType = u.ServiceType,
                    Region = u.Region,
                    RegistrationNumber = u.RegistrationNumber,
                    ImageUrl = u.ImageUrl // إرجاع الصورة للبروفايل
                })
                .FirstOrDefaultAsync();

            if (userProfile == null)
            {
                return NotFound(new { message = "عفواً، لم يتم العثور على بيانات هذا المستخدم!" });
            }

            return Ok(userProfile);
        }

        // 4. تعديل البيانات الشخصية بشكل ديناميكي
        [Authorize]
        [HttpPut("update-profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new { message = "المستخدم غير معرف أو الصلاحية غير صالحة." });
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null)
            {
                return NotFound(new { message = "عفواً، المستخدم غير موجود بالنظام!" });
            }

            // تحديث الحقول إذا كانت تحتوي على قيم
            if (!string.IsNullOrWhiteSpace(dto.FullName)) user.FullName = dto.FullName;
            if (!string.IsNullOrWhiteSpace(dto.PhoneNumber)) user.PhoneNumber = dto.PhoneNumber;
            if (dto.Age.HasValue && dto.Age.Value > 0) user.Age = dto.Age.Value;
            if (!string.IsNullOrWhiteSpace(dto.ChurchName)) user.ChurchName = dto.ChurchName;
            if (!string.IsNullOrWhiteSpace(dto.ServiceType)) user.ServiceType = dto.ServiceType;
            if (!string.IsNullOrWhiteSpace(dto.Region)) user.Region = dto.Region;

            // تحديث رابط الصورة ديناميكياً عند رفعه من فلاتر
            if (!string.IsNullOrWhiteSpace(dto.ImageUrl)) user.ImageUrl = dto.ImageUrl;

            await _context.SaveChangesAsync();

            return Ok(new { message = "تم تحديث البيانات الشخصية بنجاح!" });
        }

        // 5. تغيير الرقم السري
        [Authorize]
        [HttpPut("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new { message = "المستخدم غير معرف أو الصلاحية غير صالحة." });
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null)
            {
                return NotFound(new { message = "عفواً، المستخدم غير موجود بالنظام!" });
            }

            if (!BCrypt.Net.BCrypt.Verify(dto.OldPassword, user.PasswordHash))
            {
                return BadRequest(new { message = "عفواً، الرقم السري الحالي الذي أدخلته غير صحيح!" });
            }

            if (dto.OldPassword == dto.NewPassword)
            {
                return BadRequest(new { message = "الرقم السري الجديد يجب أن يكون مختلفاً عن الرقم السري الحالي!" });
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            await _context.SaveChangesAsync();

            return Ok(new { message = "تم تغيير الرقم السري بنجاح!" });
        }

        // 6. جلب بيانات كارت الحضور الرقمي (شاملة الصورة من أجل الفلاتر)
        [Authorize]
        [HttpGet("create-id-card")]
        public async Task<IActionResult> CreateIdCard()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new { message = "المستخدم غير معرف أو الصلاحية غير صالحة." });
            }

            var idCardData = await _context.Users
                .Where(u => u.UserId == userId)
                .Select(u => new IdCardDto
                {
                    FullName = u.FullName,
                    RegistrationNumber = u.RegistrationNumber,
                    ChurchName = u.ChurchName,
                    ServiceType = u.ServiceType,
                    Region = u.Region,
                    ImageUrl = u.ImageUrl // الصورة جاهزة للعرض داخل الكارت بالفلاتر
                })
                .FirstOrDefaultAsync();

            if (idCardData == null)
            {
                return NotFound(new { message = "عفواً، لم يتم العثور على بيانات هذا المشترك!" });
            }

            return Ok(idCardData);
        }

        // 7. رفع وتغيير الصورة الشخصية كملف فعلي برابط كامل ومباشر أونلاين
        [Authorize]
        [HttpPut("change-profile-image")]
        [Consumes("multipart/form-data")] // عشان السواجر يقلب لخانة رفع ملفات
        public async Task<IActionResult> ChangeProfileImage([FromForm] UploadImageDto dto)
        {
            // 1. استخراج الـ ID بأمان من الـ Token
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new { message = "المستخدم غير معرف أو الصلاحية غير صالحة." });
            }

            // 2. التأكد من أن الملف تم إرساله وحجمه أكبر من صفر
            if (dto.File == null || dto.File.Length == 0)
            {
                return BadRequest(new { message = "برجاء رفع ملف صورة صالحة." });
            }

            // 3. التحقق من امتداد الصورة (مختلف الأنواع المطلوبة)
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var extension = Path.GetExtension(dto.File.FileName).ToLower();

            if (!allowedExtensions.Contains(extension))
            {
                return BadRequest(new { message = "صيغة الملف غير مدعومة! الصيغ المتاحة هي: JPG, JPEG, PNG, GIF" });
            }

            // 4. جلب المستخدم من قاعدة البيانات
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null)
            {
                return NotFound(new { message = "عفواً، المستخدم غير موجود بالنظام!" });
            }

            // 5. تجهيز مسار الحفظ بأمان على السيرفر الأونلاين داخل مجلد wwwroot/uploads
            var webRoot = _env.WebRootPath;
            if (string.IsNullOrEmpty(webRoot))
            {
                webRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            }

            var uploadsFolder = Path.Combine(webRoot, "uploads");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder); // إنشائه لو مش موجود
            }

            // توليد اسم فريد للصورة عشان مفيش صورة تغطي على تانية
            var uniqueFileName = $"profile_{userId}_{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            // 6. حفظ الملف فعلياً على السيرفر الأونلاين
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await dto.File.CopyToAsync(stream);
            }

            // 7. توليد المسار الكامل والمباشر الحقيقي بدلاً من النسبي
            var domainName = $"{Request.Scheme}://{Request.Host.Value}";
            var absoluteImageUrl = $"{domainName}/uploads/{uniqueFileName}";

            // حفظ الرابط الكامل المباشر في قاعدة البيانات
            user.ImageUrl = absoluteImageUrl;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "تم رفع وتغيير الصورة الشخصية بنجاح!",
                imageUrl = absoluteImageUrl // سيرجع أونلاين كامل للفلاتر: https://...
            });
        }


        private string GenerateJwtToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"]);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[] {
                    new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Role, user.Role)
                }),
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