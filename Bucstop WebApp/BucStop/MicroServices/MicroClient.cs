using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Text;
using BucStop.Models;
using BucStop.Controllers;


namespace BucStop
{
    public class MicroClient
    {
        private readonly JsonSerializerOptions options = new JsonSerializerOptions()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        private readonly HttpClient client;
        private readonly ILogger<MicroClient> _logger;
        private List<Game> gamesList;
        private Task<List<Game>> gamesTask;

        public MicroClient(HttpClient client, ILogger<MicroClient> logger)
        {
            this.client = client;
            this._logger = logger;

            //Start Asynchronous pull of Games
            gamesTask = GetGamesWithInfo();
        }

        /// <summary>
        /// Requests the Gateway for a List of Game Information 
        /// </summary>
        /// <returns>A list GameInfo</returns>
        public async Task<GameInfo[]> GetGamesAsync()
        {
            try
            {
                var responseMessage = await this.client.GetAsync("/Gateway");

                if (responseMessage != null)
                {
                    var stream = await responseMessage.Content.ReadAsStreamAsync();
                    return await JsonSerializer.DeserializeAsync<GameInfo[]>(stream, options);
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError("{Category}: API request failed: {ErrorMessage}", "APIRequests", ex.Message);
            }
            return new GameInfo[] { };
        }

        // Converts the GameInfo objects gathered from GetGamesAsync() into Game objects to pass to controllers.
        public async Task<List<Game>> GetGamesWithInfo()
        {
            List<Game> games = new List<Game>();

            try
            {
                GameInfo[] gameInfos = await GetGamesAsync();

                if (gameInfos.Length > 0)
                {
                    _logger.LogInformation("Successfully retrieved {Count} games from API.", gameInfos.Length);
                }
                else
                {
                    _logger.LogWarning("API returned 0 games.");
                }

                foreach (GameInfo info in gameInfos)
                {
                    Game game = new Game();

                    if (info != null)
                    {
                        game.Id = info.Id;
                        game.Title = info.Title;
                        game.Content = info.Content;
                        game.Thumbnail = info.Thumbnail;
                        game.Author = info.Author;
                        game.HowTo = info.HowTo;
                        game.DateAdded = info.DateAdded;
                        game.Description = $"{info.Description} \n {info.DateAdded}";
                        game.LeaderBoard = info.LeaderBoard;

                        _logger.LogInformation("Game ID {Id} Content URL: {Content}", info.Id, info.Content);

                    }

                    games.Add(game);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving game information from API.");
            }

            return games;
        }

        // Return the private gamesList object.
        public List<Game> GetGamesList()
        {
            gamesList = gamesTask.Result;
            return this.gamesList;
        }

        /*
         * Generic method to send data back to the microservice using the HttpClient Class 
         * baseUrl - This parameter is the base URL of the microservice. For example "http://microservice.url"
         * endpoint - This parameter represents the specific endpoint or route within your microservice API that you want to send the data to. Ex. 'POST /update/data' 
         * Made with ChatGPT
         */
        public async Task<bool> SendDataAsync<T>(string baseUrl, string endpoint, T data)
        {
            try
            {
                //Set the base address of the microservice 
                client.BaseAddress = new Uri(baseUrl);

                //Serialize the data 
                string jsonData = JsonSerializer.Serialize(data);

                //Convert serialized data to bytes 
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

                //Send data using POST request
                var response = await client.PostAsync(endpoint, content);

                //Return status code (True if HTTP status code in range 200-299, false otherwise) 
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex) //Log error and return false if any exception occurs
            {
                _logger.LogError(ex.Message);
                return false; 
            }
        }
    }
}