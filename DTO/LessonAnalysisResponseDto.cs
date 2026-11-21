namespace ServiceNotebook.DTOs;

// الـ Response اللي هيرجع للفلاتر بعد تحليل جيميناي
public class LessonAnalysisResponseDto
{
    public string Feedback { get; set; } = string.Empty;
    public List<string> Games { get; set; } = new();
    public List<string> Verses { get; set; } = new();
    public List<string> Stories { get; set; } = new();
}