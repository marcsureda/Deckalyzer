using Microsoft.AspNetCore.Mvc;
using MTGDeckAnalyzer.Api.Models;
using MTGDeckAnalyzer.Api.Services;

namespace MTGDeckAnalyzer.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DeckController : ControllerBase
{
    private readonly DeckAnalysisService _analysisService;
    private readonly ILogger<DeckController> _logger;

    public DeckController(DeckAnalysisService analysisService, ILogger<DeckController> logger)
    {
        _analysisService = analysisService;
        _logger = logger;
    }

    [HttpPost("analyze")]
    public async Task<ActionResult<DeckAnalysisResult>> AnalyzeDeck([FromBody] DeckAnalysisRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.DeckList))
        {
            return BadRequest(new { error = "Deck list cannot be empty." });
        }

        try
        {
            var result = await _analysisService.AnalyzeDeck(request.DeckList);
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
