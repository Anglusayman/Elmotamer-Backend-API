using System;
using System.ComponentModel.DataAnnotations;

namespace Elmotamer.Models
{
    public class ConferenceFile
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Title { get; set; }        // عنوان الملف (مثل: محاضرة اليوم الأول)

        [Required]
        public string FileType { get; set; }     // نوع الملف (لازم يكون audio أو pdf)

        [Required]
        public string FileUrl { get; set; }      // الرابط المباشر للملف اللي الفلوتر هيشغل منه

        [Required]
        public int UploadedByAdminId { get; set; } // الـ ID الخاص بالأدمن من جدول Admins

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}