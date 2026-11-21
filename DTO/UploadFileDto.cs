using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Elmotamer.DTOs
{
    public class UploadFileDto
    {
        [Required(ErrorMessage = "برجاء اختيار الملف أولاً.")]
        public IFormFile File { get; set; }

        [Required(ErrorMessage = "برجاء إدخال عنوان للملف.")]
        public string Title { get; set; }

        // تم حذف خانة FileType تماماً لراحة الأدمن!
    }
}