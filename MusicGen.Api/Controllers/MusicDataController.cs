using Microsoft.AspNetCore.Mvc;
using MusicGen.Core;

namespace MusicGen.Api.Controllers
{
    [ApiController]
    [Route("api/music")]
    public class MusicDataController : ControllerBase
    {
        private readonly MusicDataGenerator _generator;

        public MusicDataController(MusicDataGenerator generator)
        {
            _generator = generator;
        }

        [HttpGet("data")]
        public IActionResult GetData(
            [FromQuery] long seed,
            [FromQuery] int skip = 0,
            [FromQuery] int take = 10,
            [FromQuery] string language = "en",
            [FromQuery] double likeFilter = -1
        )
        {
            var data = _generator.Generate(seed, skip, take, language, likeFilter);
            return Ok(data);
        }
    }
}
