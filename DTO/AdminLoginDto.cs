using System.ComponentModel.DataAnnotations;

namespace Elmotamer.DTOs
{
    public class AdminLoginDto
    {
        [Required(ErrorMessage = "برجاء إدخال البريد الإلكتروني للأدمن.")]
        [EmailAddress(ErrorMessage = "البريد الإلكتروني غير صالح.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "برجاء إدخال كلمة المرور.")]
        public string Password { get; set; }
    }

    public class RegisterAdminDto
    {
        [Required(ErrorMessage = "برجاء إدخال البريد الإلكتروني.")]
        [EmailAddress(ErrorMessage = "البريد الإلكتروني غير صالح.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "برجاء إدخال كلمة المرور.")]
        [MinLength(6, ErrorMessage = "كلمة المرور يجب ألا تقل عن 6 أحرف.")]
        public string Password { get; set; }

        [Required(ErrorMessage = "برجاء إدخال الاسم بالكامل.")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "برجاء إدخال رقم الهاتف.")]
        [Phone(ErrorMessage = "رقم الهاتف غير صالح.")]
        public string PhoneNumber { get; set; }

        [Range(18, 100, ErrorMessage = "السن يجب أن يكون بين 18 و 100 سنة.")]
        public int Age { get; set; }

        [Required(ErrorMessage = "برجاء تحديد صلاحية الحساب (Role).")]
        public string Role { get; set; } // "Admin" أو "SuperAdmin"
    }
}