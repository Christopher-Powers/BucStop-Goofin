using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace Snake
{
    [ApiController]
    [Route("[controller]")]
    public class SnakeController : ControllerBase
    {
        private readonly ILogger<SnakeController> _logger;
        private readonly IConfiguration _config;
        private static string gameURL;


        public SnakeController(ILogger<SnakeController> logger, IConfiguration config)
        {
            _logger = logger;
            _config = config;
            gameURL = _config["MicroserviceUrls:Snake"];
        }


        private static readonly List<GameInfo> TheInfo = new List<GameInfo>
        {
            new GameInfo {
                Id = 1,
                Title = "Snake",
                //Content = "~/js/snake.js",
                //Content = "https://localhost:1948/js/snake.js",
                Content = gameURL, // id 1
                Author = "Fall 2023 Semester",
                DateAdded = "",
                Description = "Snake is a classic arcade game that challenges the player to control a snake-like creature that grows longer as it eats apples. The player must avoid hitting the walls or the snake's own body, which can end the game.\r\n",
                HowTo = "Control with arrow keys.",
                Thumbnail = "/images/snake.jpg" //640x360 resolution
            }

        };

        [HttpGet]
        public IEnumerable<GameInfo> Get()
        {
            // Confirm the Content URL is assigned if it was not available when initialized
            if (TheInfo[0].Content == null)
            {
                TheInfo[0].Content = gameURL;
            }
            return TheInfo;
        }
    }
}