using System;
using System.ComponentModel.DataAnnotations;

namespace Elmotamer.Models
{
    public class Admin
    {
        [Key]
        public int AdminId { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string PhoneNumber { get; set; }

        [Required]
        public string PasswordHash { get; set; }

        [Required]
        public string FullName { get; set; }

        public int Age { get; set; }

        [Required]
        public string Role { get; set; } // هنا هيتخزن "Admin" أو "SuperAdmin"

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}