using Elmotamer.DbContext; // ✅ صح
using Elmotamer.DTOs;
using Elmotamer.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;
using UglyToad.PdfPig; // قراءة الـ PDF

namespace Elmotamer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ConferenceCurriculumController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ConferenceCurriculumController> _logger;

        public ConferenceCurriculumController(
            ApplicationDbContext context,
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<ConferenceCurriculumController> logger)
        {
            _context = context;
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// يستقبل ملف/نص المؤتمر + تاريخ المؤتمر + اسم المؤتمر من الأدمن،
        /// يمسح البيانات القديمة بالكامل، يولد 30 نقطة عبر الـ AI،
        /// ويخزنها بـ CreatedAt حديث، ثم يرجع الـ 30 نقطة للسوبر أدمن لعرضها ونسخها.
        /// </summary>
        [HttpPost("generate-and-save-from-file")]
        [Authorize(Roles = "SuperAdmin")] // 🔒 للسوبر أدمن فقط
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> GenerateAndSaveFromFile([FromForm] GenerateFromDocumentRequestDto request)
        {
            // التحقق من اسم المؤتمر
            if (string.IsNullOrWhiteSpace(request.ConferenceTitle))
            {
                return BadRequest(new { Message = "برجاء إدخال اسم المؤتمر." });
            }

            // 📅 إذا لم يتم إدخال تاريخ أو كان افتراضياً، نحدد البداية لشهر 6 (22 يونيو 2026)
            DateTime conferenceStartDate = request.ConferenceDate != default
                ? request.ConferenceDate
                : new DateTime(2026, 6, 22, 0, 0, 0, DateTimeKind.Utc);

            // 1. استخراج النص سواء من ملف PDF/TXT أو نص مباشر
            string extractedText = string.Empty;

            if (request.DocumentFile != null && request.DocumentFile.Length > 0)
            {
                var ext = Path.GetExtension(request.DocumentFile.FileName).ToLower();

                if (ext == ".pdf")
                {
                    extractedText = ExtractTextFromPdf(request.DocumentFile);
                }
                else if (ext == ".txt")
                {
                    using var reader = new StreamReader(request.DocumentFile.OpenReadStream(), Encoding.UTF8);
                    extractedText = await reader.ReadToEndAsync();
                }
                else
                {
                    return BadRequest(new { Message = "الملف غير مدعوم! يرجى رفع ملف .pdf أو .txt" });
                }
            }
            else if (!string.IsNullOrWhiteSpace(request.PlainTextContent))
            {
                extractedText = request.PlainTextContent;
            }

            if (string.IsNullOrWhiteSpace(extractedText))
            {
                return BadRequest(new { Message = "لم يتم العثور على أي نص داخل الملف أو حقل النص." });
            }

            try
            {
                // 🧹 2. مسح كافة النقاط القديمة تماماً من قاعدة البيانات مهما كان اسم المؤتمر
                var allExistingPoints = await _context.SpiritualDailyPoints.ToListAsync();
                if (allExistingPoints.Any())
                {
                    _context.SpiritualDailyPoints.RemoveRange(allExistingPoints);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("تم مسح كافة النقاط القديمة من قاعدة البيانات وعددهم: {Count}", allExistingPoints.Count);
                }

                // 3. إرسال النص لـ Gemini لاستخراج الـ 30 نقطة
                var generatedPoints = await CallGeminiApiAsync(extractedText);

                if (generatedPoints == null || !generatedPoints.Any())
                {
                    return StatusCode(500, new { Message = "فشل الذكاء الاصطناعي في استخراج المنهج الروحي." });
                }

                // 4. تحويل النقاط لـ Entities وتسجيل تاريخ الإضافة الحقيقي (CreatedAt)
                DateTime nowUtc = DateTime.UtcNow;
                var entitiesToSave = generatedPoints.Select(p => new SpiritualDailyPoint
                {
                    ConferenceTitle = request.ConferenceTitle,
                    ConferenceDate = conferenceStartDate,
                    DayNumber = p.DayNumber,
                    CorePoint = p.CorePoint,
                    BibleVerse = p.BibleVerse,
                    PatristicQuote = p.PatristicQuote,
                    IllustrativeStory = p.IllustrativeStory,
                    PracticalStep = p.PracticalStep,
                    CreatedAt = nowUtc // 👈 تاريخ إضافة النقاط الحالي ليكون بداية العداد
                }).ToList();

                // 5. حفظ الـ 30 Row دفعة واحدة في الداتابيز
                await _context.SpiritualDailyPoints.AddRangeAsync(entitiesToSave);
                await _context.SaveChangesAsync();

                _logger.LogInformation("تم حفظ {Count} يوم للمؤتمر: {Title} بتاريخ: {Date}",
                    entitiesToSave.Count, request.ConferenceTitle, conferenceStartDate.ToShortDateString());

                // 6. تجهيز قائمة الـ DTO المكتملة بالـ 30 نقطة المخزنة لإرجاعها للسوبر أدمن لنسخها في الـ UI
                var savedResponseList = entitiesToSave.Select(point => new SpiritualDailyPointResponseDto
                {
                    Id = point.Id,
                    ConferenceTitle = point.ConferenceTitle,
                    ConferenceDate = point.ConferenceDate,
                    DayNumber = point.DayNumber,
                    CorePoint = point.CorePoint,
                    BibleVerse = point.BibleVerse,
                    PatristicQuote = point.PatristicQuote,
                    IllustrativeStory = point.IllustrativeStory,
                    PracticalStep = point.PracticalStep
                }).ToList();

                return Ok(new
                {
                    Success = true,
                    Message = $"تم مسح النقاط القديمة بالكامل، واستخراج وحفظ {entitiesToSave.Count} يوم بنجاح للمؤتمر!",
                    ConferenceTitle = request.ConferenceTitle,
                    ConferenceDate = conferenceStartDate,
                    TotalPoints = savedResponseList.Count,
                    Data = savedResponseList // 👈 إرجاع النقاط الـ 30 المخزنة كاملة
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ أثناء معالجة الملف عبر الـ AI.");
                return StatusCode(500, new { Message = "حدث خطأ في السيرفر أثناء المعالجة.", Details = ex.Message });
            }
        }

        /// <summary>
        /// استرجاع نقطة اليوم للمخدوم بناءً على تاريخ جهازه (clientDate):
        /// - الأيام من 1 إلى 30: يُرجع النقطة المحددة بالترتيب مقارنةً بـ CreatedAt.
        /// - من اليوم 31 فأكثر: يُرجع نقطة عشوائية (Random) من النقاط المتاحة.
        /// </summary>
        [HttpGet("today-point")]
        public async Task<IActionResult> GetTodayPoint([FromQuery] DateTime clientDate) // 👈 استلام تاريخ موبايل المخدوم فقط
        {
            // 1. جلب أول تاريخ تم فيه إضافة النقاط بالجدول (CreatedAt)
            var firstCreationDate = await _context.SpiritualDailyPoints
                .AsNoTracking()
                .OrderBy(p => p.CreatedAt)
                .Select(p => p.CreatedAt.Date)
                .FirstOrDefaultAsync();

            if (firstCreationDate == default)
            {
                return NotFound(new { Message = "لم يتم العثور على أي نقاط روحية في قاعدة البيانات." });
            }

            // 2. حساب رقم اليوم: (تاريخ جهاز المخدوم - تاريخ إضافة النقطة) + 1
            DateTime userDateOnly = clientDate.Date;
            int calculatedDayNumber = (userDateOnly - firstCreationDate).Days + 1;

            if (calculatedDayNumber < 1)
            {
                return BadRequest(new { Message = "النقاط غير متاحة بعد لهذا التاريخ." });
            }

            SpiritualDailyPoint? point = null;

            // 3. المنطق: من 1 إلى 30 بالترتيب | من 31 فأكثر نقطة عشوائية (Random)
            if (calculatedDayNumber <= 30)
            {
                // بحث برقم اليوم فقط (مفيش داعي لاسم المؤتمر لأن الداتابيز فيها المؤتمر الحالي بس)
                point = await _context.SpiritualDailyPoints
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.DayNumber == calculatedDayNumber);
            }
            else
            {
                // اختيار نقطة عشوائية من الـ 30 نقطة الموجودين بالجدول
                point = await _context.SpiritualDailyPoints
                    .AsNoTracking()
                    .OrderBy(p => EF.Functions.Random())
                    .FirstOrDefaultAsync();
            }

            if (point == null)
            {
                return NotFound(new { Message = "لم يتم العثور على نقطة روحية متاحة حالياً." });
            }

            // 4. استخدام الـ Response DTO لمنع تسريب بيانات حقول السيرفر
            var responseDto = new SpiritualDailyPointResponseDto
            {
                Id = point.Id,
                ConferenceTitle = point.ConferenceTitle,
                ConferenceDate = point.ConferenceDate,
                DayNumber = point.DayNumber,
                CorePoint = point.CorePoint,
                BibleVerse = point.BibleVerse,
                PatristicQuote = point.PatristicQuote,
                IllustrativeStory = point.IllustrativeStory,
                PracticalStep = point.PracticalStep
            };

            return Ok(responseDto);
        }

        #region Private Helpers

        private string ExtractTextFromPdf(IFormFile file)
        {
            var sb = new StringBuilder();
            using var stream = file.OpenReadStream();
            using var pdf = PdfDocument.Open(stream);

            foreach (var page in pdf.GetPages())
            {
                sb.AppendLine(page.Text);
            }

            return sb.ToString();
        }

        private async Task<List<GeneratedPointDto>> CallGeminiApiAsync(string textContent)
        {
            // ✅ نفس مطابقة قراءة الـ API Key في الـ LessonAnalysisService بالظبط
            var apiKey = _configuration["Gemini:ApiKey"]
                      ?? _configuration["Gemini__ApiKey"]
                      ?? _configuration["GeminiApiKey"]
                      ?? Environment.GetEnvironmentVariable("Gemini__ApiKey")
                      ?? Environment.GetEnvironmentVariable("GeminiApiKey");

            if (string.IsNullOrEmpty(apiKey))
            {
                _logger.LogError("Gemini API Key is missing in Configuration or Environment Variables.");
                throw new InvalidOperationException("GeminiApiKey غير معرف في appsettings.json أو إعدادات السيرفر.");
            }

            string systemPrompt = @"أنت خبير لاهوتي ومربي روحي في الكنيسة القبطية الأرثوذكسية.
اقرأ نص المؤتمر المرفق، واستخرج منه منهجاً روحياً مكوناً من 30 يوماً متسلسلاً بأسلوب أرثوذكسي عميق.

لكل يوم من الـ 30 يوماً، استخرج الأجزاء التالية بدقة:
- dayNumber: رقم اليوم (من 1 إلى 30).
- corePoint: النقطة الأصلية / الجوهرية (الفكرة الرئيسية مستخرجة بأسلوب مشجع).
- bibleVerse: الشاهد الكتابي والآية المؤيدة.
- patristicQuote: التأمل الروحي / قول آبائي أرثوذكسي تناسب الفكرة.
- illustrativeStory: القصة التوضيحية أو التشبيه الروحي من بستان الرهبان أو التراث الكنسي.
- practicalStep: التدريب العملي الروحي المحدد لهذا اليوم.

التزم بإرجاع JSON Array صريح ينطبق تماماً مع هذا الشكل بدون أي مقدمات أو مؤخرات:
[
  {
    ""dayNumber"": 1,
    ""corePoint"": ""..."",
    ""bibleVerse"": ""..."",
    ""patristicQuote"": ""..."",
    ""illustrativeStory"": ""..."",
    ""practicalStep"": ""...""
  }
]";

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = $"{systemPrompt}\n\n--- نص المؤتمر ---\n{textContent}" }
                        }
                    }
                },
                generationConfig = new
                {
                    temperature = 0.2,
                    responseMimeType = "application/json"
                }
            };

            // ✅ استخدام نفس الرابط الشغال في الـ Service
            var requestUrl = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-flash-latest:generateContent?key={apiKey}";
            var response = await _httpClient.PostAsJsonAsync(requestUrl, requestBody);

            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Gemini API Error: {response.StatusCode} - {err}");
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseJson);

            var rawJsonText = doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString() ?? "[]";

            // 👈 تنظيف الـ String في حال إرجاع AI لـ Markdown backticks
            rawJsonText = rawJsonText.Replace("```json", "").Replace("```", "").Trim();

            var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return JsonSerializer.Deserialize<List<GeneratedPointDto>>(rawJsonText, jsonOptions) ?? new List<GeneratedPointDto>();
        }

        #endregion
    }
}