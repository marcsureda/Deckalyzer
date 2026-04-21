using Microsoft.AspNetCore.Mvc;
using MTGDeckAnalyzer.Application.Models;
using MTGDeckAnalyzer.Application.Services;

namespace MTGDeckAnalyzer.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PreconController : ControllerBase
{
    private readonly IPreconService _preconService;

    public PreconController(IPreconService preconService)
    {
        _preconService = preconService;
    }

    [HttpGet("search")]
    public async Task<ActionResult<PreconSearchResult>> SearchPrecons(
        [FromQuery] string? query = null,
        [FromQuery] string? year = null,
        [FromQuery] string[]? colors = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var result = await _preconService.SearchPreconsAsync(query, year, colors, page, pageSize);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Failed to search precons", details = ex.Message });
        }
    }

    [HttpGet("{name}")]
    public async Task<ActionResult<PreconDeck>> GetPrecon(string name)
    {
        try
        {
            var precon = await _preconService.GetPreconByNameAsync(name);
            if (precon == null)
            {
                return NotFound(new { error = "Precon deck not found" });
            }
            return Ok(precon);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Failed to get precon", details = ex.Message });
        }
    }
}