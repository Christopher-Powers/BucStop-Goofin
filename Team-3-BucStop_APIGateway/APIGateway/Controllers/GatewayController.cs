using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text;

namespace Gateway
{
    /// <summary>
    /// Controller responsible for handling requests and responses at the gateway.
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class GatewayController : ControllerBase
    {
        private readonly HttpClient _httpClient; // Making readonly ensures thread safety 
        private readonly ILogger<GatewayController> _logger; // Making readonly ensures thread safety 
        private readonly List<GameInfo> TheInfo;

        public GatewayController(HttpClient httpClient, ILogger<GatewayController> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            TheInfo = new List<GameInfo>();
        }

        /// <summary>
        /// Handles GET requests to retrieve game information from microservices.
        /// </summary>
        /// <returns>A collection of GameInfo objects.</returns>
        [HttpGet]
        public async Task<IEnumerable<GameInfo>> Get()
        {
            try
            {
                var SnakeTask = AddGameInfo("http://3.232.16.65", "/Snake" ); //Snake 
                var PongTask = AddGameInfo("http://3.232.16.65", "/Pong"); //Pong
                var TetrisTask = AddGameInfo("http://3.232.16.65", "/Tetris"); //Tetris
                await Task.WhenAll(SnakeTask, PongTask, TetrisTask);
                return TheInfo;
            }
            catch (Exception ex)
            {
                _logger.LogError($"An error occurred while fetching data from microservice: {ex.Message}");
                // Return a placeholder list of GameInfo objects indicating failure
                return GenerateFailureResponse();
            }
        }

         /// <summary>
         /// Attempts to retrieve gameinfo object from a microservice that holds a game info object (snake, tetris, pong)
         /// and adds the game info into a list of game info objects
         /// </summary>
         /// <param name="gameinfolist"></param>
         /// <param name="baseUrl"></param>
         /// <param name="endpoint"></param>
         /// <returns></returns>
         [ApiExplorerSettings(IgnoreApi = true)]
        public async Task AddGameInfo(string baseUrl, string endpoint)
        {
            try
            {
                using var client = new HttpClient();
                //Set the base address of the microservice 
                client.BaseAddress = new Uri(baseUrl);

                //Read the data from the endpoint 
                HttpResponseMessage response = await client.GetAsync(endpoint);

                // Check if the request was successful
                if (response.IsSuccessStatusCode)
                {
                    // Deserialize the response content to a GameInfo object
                    var gameinfo = await response.Content.ReadAsAsync<List<GameInfo>>();
                    //Add object to list
                    lock (TheInfo)
                    {
                        TheInfo.AddRange(gameinfo);
                    }
                }
                else
                {
                    _logger.LogError($"Failed to retrieve data from microservice at endpoint {endpoint}. Status code: {response.StatusCode}");
                }

            }
            catch (Exception ex) //Log error and return false if any exception occurs
            { 
                _logger.LogError(ex.Message);
            }
        }

        /// <summary>
        /// Generates a placeholder list of GameInfo objects indicating failure to retrieve data.
        /// Written with ChatGPT
        /// </summary>
        /// <returns>A collection of GameInfo objects indicating failure.</returns>
        private IEnumerable<GameInfo> GenerateFailureResponse()
        {
            // Using IEnumerable allows flexibility in returning a placeholder response.
            // It allows the method to return different types of collections (e.g., List, Array) if needed in the future.
            // In this case, IEnumerable provides a simple way to return a list of failed responses.

            // Generate a placeholder list of GameInfo objects indicating failure to retrieve data
            return new List<GameInfo>
            {
                new GameInfo
                {
                    Title = "Failed to retrieve from Microservice",
                    Author = "Failed to retrieve from Microservice",
                    Description = "Failed to retrieve from Microservice",
                    HowTo = "Failed to retrieve from Microservice",
                    LeaderBoardStack = new Stack<KeyValuePair<string, int>>() // Initializing an empty stack
                }
            };
        }
    }
}
