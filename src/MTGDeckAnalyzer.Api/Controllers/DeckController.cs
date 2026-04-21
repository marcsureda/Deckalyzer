using Microsoft.AspNetCore.Mvc;
using MTGDeckAnalyzer.Application.Models;
using MTGDeckAnalyzer.Application.Services;

namespace MTGDeckAnalyzer.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DeckController : ControllerBase
{
    private readonly IDeckAnalysisService _analysisService;
    private readonly ILogger<DeckController> _logger;

    public DeckController(IDeckAnalysisService analysisService, ILogger<DeckController> logger)
    {
        _analysisService = analysisService;
        _logger = logger;
    }

    [HttpPost("analyze")]
    public async Task<ActionResult<DeckAnalysisResult>> AnalyzeDeck([FromBody] DeckAnalysisRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.DeckList))
        {
            return BadRequest(new { error = "Deck list cannot be empty." });
        }

        try
        {
            var result = await _analysisService.AnalyzeDeck(request.DeckList, cancellationToken);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing deck");
            return StatusCode(500, new { error = "An error occurred while analyzing the deck." });
        }
    }
}
