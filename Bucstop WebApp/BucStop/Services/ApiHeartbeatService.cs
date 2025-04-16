using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace BucStop.Services
{
    public class ApiHeartbeatService : BackgroundService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ApiHeartbeatService> _logger;

        // Define each service name -> health‑check URL here
        private readonly Dictionary<string, string> _servicesToCheck = new()
        {
            { "API Gateway", "https://localhost:4141/health" },
            { "Snake",       "https://localhost:1948/health" },
            { "Tetris",      "https://localhost:2626/health" }, 
            { "Pong",        "https://localhost:1941/health" },
        };

        public ApiHeartbeatService(HttpClient httpClient, ILogger<ApiHeartbeatService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                foreach (var (name, url) in _servicesToCheck)
                {
                    try
                    {
                        var response = await _httpClient.GetAsync(url, stoppingToken);
                        if (response.IsSuccessStatusCode)
                        {
                            _logger.LogInformation("Heartbeat: {Service} is healthy.", name);
                        }
                        else
                        {
                            _logger.LogWarning("Heartbeat: {Service} returned {StatusCode}.", name, response.StatusCode);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("Heartbeat: {Service} check failed – {Error}.", name, ex.Message);
                    }
                }

                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken); // Runs every 5 minutes
            }
        }
    }
}
