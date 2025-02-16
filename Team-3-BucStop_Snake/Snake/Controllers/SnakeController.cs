using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace Snake
{
    [ApiController]
    [Route("[controller]")]
    public class SnakeController : ControllerBase
    {
        private readonly GameDbContext _context;
        private readonly ILogger<SnakeController> _logger;

        public SnakeController(GameDbContext context, ILogger<SnakeController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public ActionResult<IEnumerable<GameInfo>> Get()
        {
            try
            {
                var games = _context.Games.Where(g => g.Title == "Snake").ToList();
                if (!games.Any()) return NotFound("No Snake game data found.");

                return Ok(games);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error retrieving Snake game data: {0}", ex.Message);
                return StatusCode(500, "Internal server error while retrieving game data.");
            }
        }
    }
}
