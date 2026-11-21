using System.ComponentModel.DataAnnotations;

namespace Elmotamer.DTOs;

public record LessonAnalysisRequestDto(
    [Required(ErrorMessage = "عنوان الدرس مطلوب ولا يمكن تركه فارغاً.")]
    string LessonTitle,

    [Required(ErrorMessage = "اسم الخدمة مطلوب")]
    string ServiceName,

    // تم إلغاء الـ Required والـ MinLength للسماح للسيناريو التلقائي (التوليد من الصفر)
    string? LessonContent
);