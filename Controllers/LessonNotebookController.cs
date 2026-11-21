namespace Elmotamer.Controllers;

using Elmotamer.DTOs;
using Elmotamer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceNotebook.DTOs;

[ApiController]
[Route("api/[controller]")]
[Authorize] // 🔐 حماية بالـ JWT Token عشان محدش يستدعيه غير الي عامل تسجيل دخول
public class LessonNotebookController : ControllerBase
{
    private readonly ILessonAnalysisService _analysisService;
    private readonly ILogger<LessonNotebookController> _logger;

    public LessonNotebookController(ILessonAnalysisService analysisService, ILogger<LessonNotebookController> logger)
    {
        _analysisService = analysisService;
        _logger = logger;
    }

    [HttpPost("analyze")]
    [ProducesResponseType(typeof(LessonAnalysisResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> AnalyzeLesson([FromBody] LessonAnalysisRequestDto request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var result = await _analysisService.AnalyzeLessonAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to analyze lesson notebook entry.");

            // تم التعديل مؤقتاً لـ ex.ToString() عشان يظهرلك سبب الخطأ الحقيقي في Swagger
            return Problem(
                detail: ex.ToString(),
                statusCode: StatusCodes.Status500InternalServerError,
                title: "خطأ في المعالجة"
            );
        }
    }
}