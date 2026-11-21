namespace Elmotamer.DTOs
{
    public class SuperAdminAttendeesReportDto
    {
        public int TotalAttendeesCount { get; set; }
        public List<AttendeeSummaryDto> Attendees { get; set; } = new();
    }

    public class AttendeeSummaryDto
    {
        public string FullName { get; set; } = string.Empty;
        public string RegistrationNumber { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
    }
}