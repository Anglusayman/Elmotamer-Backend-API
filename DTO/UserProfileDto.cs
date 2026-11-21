namespace Elmotamer.DTOs
{
    public class UserProfileDto
    {
        public int UserId { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string FullName { get; set; }
        public int Age { get; set; }
        public string ChurchName { get; set; }
        public string ServiceType { get; set; }
        public string Region { get; set; }
        public string RegistrationNumber { get; set; }
        // الحقل الجديد
        public string? ImageUrl { get; set; }
    }
}