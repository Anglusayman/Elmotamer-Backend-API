using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Elmotamer.DTOs
{
    public class UpdateProfileDto
    {
        [DefaultValue(null)]
        public string? FullName { get; set; } = null;

        [DefaultValue(null)]
        public string? PhoneNumber { get; set; } = null;

        
        public int? Age { get; set; } = null;

        [DefaultValue(null)]
        public string? ChurchName { get; set; } = null;

        [DefaultValue(null)]
        public string? ServiceType { get; set; } = null;

        [DefaultValue(null)]
        public string? Region { get; set; } = null;

        public string? ImageUrl { get; set; }


    }

    public class ChangePasswordDto
    {
        [Required(ErrorMessage = "يجب إدخال الرقم السري الحالي")]
        public string OldPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "يجب إدخال الرقم السري الجديد")]
        [MinLength(6, ErrorMessage = "الرقم السري الجديد لا يمكن أن يقل عن 6 أحرف")]
        public string NewPassword { get; set; } = string.Empty;
    }
    public class UploadImageDto
    {
        [Required(ErrorMessage = "برجاء اختيار ملف الصورة")]
        public IFormFile File { get; set; } = null!;
    }
}

