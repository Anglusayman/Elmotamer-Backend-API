namespace Elmotamer.DTOs
{
    public class IdCardDto
    {
        public string FullName { get; set; } = string.Empty;
        public string RegistrationNumber { get; set; } = string.Empty;
        public string ChurchName { get; set; } = string.Empty;
        public string ServiceType { get; set; } = string.Empty;
        public string Region { get; set; } = string.Empty;
        // هيرجع للفلاتر عشان يعرض صورة الخادم جوه الكارت
        public string? ImageUrl { get; set; }
        public int? SeatNumber { get; set; }
        public string? GroupName { get; set; }
        public string? GroupLocation { get; set; }
    }
}