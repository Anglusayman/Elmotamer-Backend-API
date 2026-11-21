using System;
using System.ComponentModel.DataAnnotations;

namespace Elmotamer.DTOs
{
    public class ArchiveAndResetConferenceDto
    {
        [Required(ErrorMessage = "تاريخ المؤتمر مطلوب.")]
        public DateTime ConferenceDate { get; set; }

        [Required(ErrorMessage = "يجب تأكيد إعادة الضبط.")]
        public bool ConfirmReset { get; set; }
    }
}