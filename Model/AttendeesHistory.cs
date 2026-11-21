using System;
using System.ComponentModel.DataAnnotations;

namespace Elmotamer.Models
{
    public class AttendeesHistory
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; } // 👈 التعديل الجديد

        [Required]
        [MaxLength(150)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string RegistrationNumber { get; set; } = string.Empty;

        [Required]
        public DateTime ConferenceDate { get; set; }

        public DateTime ArchivedAt { get; set; } = DateTime.UtcNow;
    }
}