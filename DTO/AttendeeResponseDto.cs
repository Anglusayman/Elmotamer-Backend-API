namespace Elmotamer.DTOs
{
    namespace Elmotamer.DTOs
    {
        public class IdCardResponseDto
        {
            public string RegistrationNumber { get; set; } = string.Empty;
            public string FullName { get; set; } = string.Empty;
            public string? ServiceType { get; set; }
            public string? Region { get; set; }
            public string? ChurchName { get; set; }
            public int Age { get; set; }
            public string FullImageUrl { get; set; } = string.Empty;
            public bool IsApproved { get; set; }

            // الحقول الجديدة للإضافة على الكارت
            public int SeatNumber { get; set; }
            public string GroupName { get; set; } = string.Empty;
            public string? GroupLocation { get; set; }
        }
    }

    // إضافة هذا الكلاس لحل مشكلة الـ AttendanceController
    public class AttendeeResponseDto
    {
        public int AttendeeId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string ChurchName { get; set; } = string.Empty;
        public string ServiceType { get; set; } = string.Empty;
        public string RegistrationNumber { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public DateTime ApprovedAt { get; set; }

        // الحقول الجديدة المرجعة
        public int SeatNumber { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public string? GroupLocation { get; set; }
    }
}