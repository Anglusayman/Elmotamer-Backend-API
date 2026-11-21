using System;

namespace Elmotamer
{
    public class User
    {
        public int UserId { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string PasswordHash { get; set; }
        public string FullName { get; set; }
        public int Age { get; set; }
        public string ChurchName { get; set; }
        public string ServiceType { get; set; }
        public string Region { get; set; }
        public string RegistrationNumber { get; set; }
        public string Role { get; set; } = "User";
        public bool IsPhoneVerified { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? ImageUrl { get; set; }

        
    }
}