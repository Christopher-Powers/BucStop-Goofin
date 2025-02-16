using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using Gateway;

namespace APIGateway
{
    [ApiController]
    [Route("api/games")]
    public class GatewayController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<GatewayController> _logger;
        private readonly IConfiguration _configuration;

        public GatewayController(IHttpClientFactory httpClientFactory, ILogger<GatewayController> logger, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _configuration = configuration;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllGameInfo()
        {
            var gameInfoList = new List<GameInfo>();

            var services = new Dictionary<string, string>
            {
                { "SnakeService", _configuration["ApiRoutes:SnakeService"] },
                { "TetrisService", _configuration["ApiRoutes:TetrisService"] },
                { "PongService", _configuration["ApiRoutes:PongService"] }
            };

            var tasks = new List<Task>();

            foreach (var service in services)
            {
                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        var client = _httpClientFactory.CreateClient();
                        var response = await client.GetAsync(service.Value);

                        if (response.IsSuccessStatusCode)
                        {
                            var content = await response.Content.ReadAsStringAsync();
                            var gameInfo = JsonSerializer.Deserialize<List<GameInfo>>(content);
                            if (gameInfo != null)
                            {
                                lock (gameInfoList) // Ensure thread safety
                                {
                                    gameInfoList.AddRange(gameInfo);
                                }
                            }
                        }
                        else
                        {
                            _logger.LogError($"Failed to retrieve data from {service.Key} - Status Code: {response.StatusCode}");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error fetching from {service.Key}: {ex.Message}");
                    }
                }));
            }

            await Task.WhenAll(tasks);

            return gameInfoList.Count > 0 ? Ok(gameInfoList) : NotFound("No game data available.");
        }
    }
}
