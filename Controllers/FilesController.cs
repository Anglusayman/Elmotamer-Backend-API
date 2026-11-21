using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Elmotamer.DbContext;
using Elmotamer.Models;
using Elmotamer.DTOs;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Elmotamer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FilesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public FilesController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // 1. رفع الملفات - أوتوماتيك ومحمي تماماً (للأدمن والسوبر أدمن)
        [Authorize(Roles = "Admin,SuperAdmin")]
        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile([FromForm] UploadFileDto dto)
        {
            if (dto.File == null || dto.File.Length == 0)
                return BadRequest(new { message = "برجاء اختيار ملف صالح." });

            // 🔥 تحديد امتداد الملف تلقائياً (مثال: .pdf أو .mp3)
            var extension = Path.GetExtension(dto.File.FileName).ToLower();
            string detectedType = "";

            // الفحص التلقائي بناءً على الامتداد
            if (extension == ".pdf")
            {
                detectedType = "pdf";
            }
            else if (extension == ".mp3" || extension == ".wav" || extension == ".m4a" || extension == ".audio")
            {
                detectedType = "audio";
            }
            else
            {
                return BadRequest(new { message = "امتداد الملف غير مدعوم! المسموح فقط ملفات PDF أو ملفات صوتية." });
            }

            // استخراج الـ AdminId من التوكن
            var adminIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(adminIdStr) || !int.TryParse(adminIdStr, out int adminId))
                return Unauthorized(new { message = "فشل التحقق من هوية الأدمن. التوكن غير صالح." });

            // التأكد 100% إن الـ ID ده موجود في جدول الـ Admins
            var adminExists = await _context.Admins.AnyAsync(a => a.AdminId == adminId);
            if (!adminExists)
                return Forbid();

            // استخدام حماية في حالة كان الـ WebRootPath بـ null على السيرفر الأونلاين
            var webRoot = _env.WebRootPath;
            if (string.IsNullOrEmpty(webRoot))
            {
                webRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            }

            var uploadsFolder = Path.Combine(webRoot, "Uploads");

            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            // توليد اسم فريد للملف
            var uniqueFileName = Guid.NewGuid().ToString() + extension;
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            // نقل وحفظ الملف الفعلي على السيرفر
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await dto.File.CopyToAsync(stream);
            }

            // 🔥 توليد الـ URL المباشر والكامل (https://domain.com/Uploads/file.pdf)
            var domainName = $"{Request.Scheme}://{Request.Host.Value}";
            var fileUrl = $"{domainName}/Uploads/{uniqueFileName}";

            // حفظ البيانات في جدول الملفات بالنوع الذي تم كشفه تلقائياً
            var conferenceFile = new ConferenceFile
            {
                Title = dto.Title,
                FileType = detectedType, // يتخزن تلقائياً "pdf" أو "audio"
                FileUrl = fileUrl,
                UploadedByAdminId = adminId
            };

            _context.ConferenceFiles.Add(conferenceFile);
            await _context.SaveChangesAsync();

            // حل الـ Crash وإرجاع البيانات الهامة
            return Ok(new
            {
                message = "تم رفع الملف بنجاح وتحديد نوعه تلقائياً!",
                file = new
                {
                    id = conferenceFile.Id,
                    title = conferenceFile.Title,
                    fileType = conferenceFile.FileType,
                    fileUrl = conferenceFile.FileUrl,
                    uploadedByAdminId = conferenceFile.UploadedByAdminId
                }
            });
        }

        // 2. جلب الملفات للمستخدمين
        [Authorize]
        [HttpGet("all")]
        public async Task<IActionResult> GetAllFiles()
        {
            var files = await _context.ConferenceFiles
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();

            return Ok(files);
        }
    }
}