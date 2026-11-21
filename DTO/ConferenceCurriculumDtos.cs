using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Elmotamer.DTOs
{
    /// <summary>
    /// البيانات التي يرسلها الـ SuperAdmin عند رفع الملف
    /// </summary>
    public class GenerateFromDocumentRequestDto
    {
        [Required(ErrorMessage = "اسم المؤتمر مطلوب")]
        public string ConferenceTitle { get; set; } = string.Empty;

        [Required(ErrorMessage = "تاريخ المؤتمر مطلوب")]
        public DateTime ConferenceDate { get; set; }

        // إما رفع نص صريح
        public string? PlainTextContent { get; set; }

        // أو رفع ملف (pdf / txt)
        public IFormFile? DocumentFile { get; set; }
    }

    /// <summary>
    /// شكل النقطة الواحدة الناتجة من AI والمخزنة في الـ Array
    /// </summary>
    public class GeneratedPointDto
    {
        public int DayNumber { get; set; }
        public string CorePoint { get; set; } = string.Empty;       // النقطة الجوهرية
        public string BibleVerse { get; set; } = string.Empty;      // آية كتابية وشاهدها
        public string PatristicQuote { get; set; } = string.Empty;  // قول آبائي / تأمل
        public string IllustrativeStory { get; set; } = string.Empty; // قصة توضيحية
        public string PracticalStep { get; set; } = string.Empty;   // تدريب عملي
    }

    /// <summary>
    /// شكل الاستجابة (Response) التي ترجع بعد العرض للمخدومين
    /// </summary>
    public class SpiritualDailyPointResponseDto
    {
        public int Id { get; set; }
        public string ConferenceTitle { get; set; } = string.Empty;
        public DateTime ConferenceDate { get; set; }
        public int DayNumber { get; set; }
        public string CorePoint { get; set; } = string.Empty;
        public string BibleVerse { get; set; } = string.Empty;
        public string PatristicQuote { get; set; } = string.Empty;
        public string IllustrativeStory { get; set; } = string.Empty;
        public string PracticalStep { get; set; } = string.Empty;
    }
}