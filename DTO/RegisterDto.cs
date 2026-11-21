namespace Elmotamer.DTOs
{
    public class RegisterDto
    {
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string Password { get; set; }
        public string FullName { get; set; }
        public int Age { get; set; }
        public string ChurchName { get; set; }
        public string ServiceType { get; set; }
        public string Region { get; set; }
        public string FirebaseToken { get; set; } // الحقل الجديد المستلم من الفلاتر
    }
}
