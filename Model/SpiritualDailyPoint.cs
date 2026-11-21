using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Elmotamer.Models
{
    [Table("SpiritualDailyPoints")]
    public class SpiritualDailyPoint
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string ConferenceTitle { get; set; } = string.Empty;

        [Required]
        public DateTime ConferenceDate { get; set; }

        [Required]
        public int DayNumber { get; set; }

        [Required]
        public string CorePoint { get; set; } = string.Empty;

        [Required]
        public string BibleVerse { get; set; } = string.Empty;

        [Required]
        public string PatristicQuote { get; set; } = string.Empty;

        [Required]
        public string IllustrativeStory { get; set; } = string.Empty;

        [Required]
        public string PracticalStep { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}