using System;
using System.ComponentModel.DataAnnotations;

namespace Elmotamer.Models
{
    public class Attendee
    {
        public int AttendeeId { get; set; }
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public int Age { get; set; }
        public string ChurchName { get; set; } = string.Empty;
        public string ServiceType { get; set; } = string.Empty;
        public string Region { get; set; } = string.Empty;
        public string RegistrationNumber { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public int ApprovedByAdminId { get; set; }
        public DateTime ApprovedAt { get; set; }

        // الحقول الجديدة
        public int SeatNumber { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public string? GroupLocation { get; set; }
    }
}
