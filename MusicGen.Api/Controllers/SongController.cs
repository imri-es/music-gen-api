using Microsoft.AspNetCore.Mvc;
using MusicGen.Core;
using MusicGen.Core.Config;

[ApiController]
[Route("api/song")]
public class SongController : ControllerBase
{
    private readonly IConfiguration _config;

    public SongController(IConfiguration config)
    {
        _config = config;
    }

    [HttpPost("generate")]
    public async Task<IActionResult> Generate(long seed, string language = "en")
    {
        var generator = new SongGenerator(_config);
        var result = await generator.GenerateAsync(seed, language);
        return Ok(result);
    }
}
