namespace Elmotamer.DTOs
{
    public class ResetPasswordDto
    {
        public string PhoneNumber { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
        public string FirebaseToken { get; set; } = string.Empty; // 👈 التوكن السري القادم من الفلاتر
    }
}