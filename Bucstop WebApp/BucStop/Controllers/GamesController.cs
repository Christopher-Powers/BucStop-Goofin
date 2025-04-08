using BucStop.Models;
using BucStop.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using System.Diagnostics;

/*
 * This file handles the links to each of the game pages.
 */

namespace BucStop.Controllers;

[Authorize]
public class GamesController : Controller
{
    private readonly MicroClient _httpClient;
    private readonly PlayCountManager _playCountManager;
    private readonly GameService _gameService;
    private readonly ILogger<GamesController> _logger;


    public GamesController(MicroClient games, IWebHostEnvironment webHostEnvironment, GameService gameService, ILogger<GamesController> logger)
    {
        _httpClient = games;
        _gameService = gameService;
        _logger = logger;

        // Initialize the PlayCountManager with the web root path and the JSON file name
        _playCountManager = new PlayCountManager(_gameService.GetGames() ?? new List<Game>(), webHostEnvironment);
    }

    //Takes the user to the index page, passing the games list as an argument
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<IActionResult> IndexAsync()
    {
        _logger.LogInformation("Games index page accessed.");
       
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();

        List<Game> games = await GetGamesWithInfo();

        //have to update playcounts here since the we are reading it dynamically now instead of from a static list
        foreach(Game game in games)
        {
            game.PlayCount = _playCountManager.GetPlayCount(game.Id);
        }

        stopwatch.Stop();

        _logger.LogInformation("{Category}: Games Page Loaded in {LoadTime}ms.", "PageLoadTimes", stopwatch.ElapsedMilliseconds);

        return View(games);
    }

    //Takes the user to the Play page, passing the game object the user wants to play
    public async Task<IActionResult> Play(int id)
    {
        _logger.LogInformation("{Category}: User requested to play game with ID {GameId}.", "GameSuccess", id);

        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();

        List<Game> games = await GetGamesWithInfo();

        Game game = games.FirstOrDefault(x => x.Id == id);
        if (game == null)
        {
            _logger.LogWarning("{Category}: Game with ID {GameId} not found.", "GameSuccess", id);
            return NotFound();
        }

        // Increment the play count for the game with the specified ID
        _playCountManager.IncrementPlayCount(id);

        int playCount = _playCountManager.GetPlayCount(id);

        // Update the game's play count
        game.PlayCount = playCount;

        _logger.LogInformation("{Category}: Game '{GameTitle}' (ID: {GameId}) successfully loaded.",
                                "GameSuccess", game.Title, game.Id);

        stopwatch.Stop();

        _logger.LogInformation("{Category}: {GameTitle} Page Loaded in {LoadTime}ms.", "PageLoadTimes", game.Title, stopwatch.ElapsedMilliseconds);

        return View(game);
    }

    public async Task<List<Game>> GetGamesWithInfo()
    {

        List<Game> games = _gameService.GetGames();
        try
        {
            GameInfo[] gameInfos = await _httpClient.GetGamesAsync();

            if (gameInfos.Length > 0)
            {
                _logger.LogInformation("Successfully retrieved {Count} games from API.", gameInfos.Length);
            }
            else
            {
                _logger.LogWarning("API returned 0 games.");
            }

            foreach (Game game in games)
            {
                GameInfo info = gameInfos.FirstOrDefault(x => x.Title == game.Title);
                if (info != null)
                {
                    game.Author = info.Author;
                    game.HowTo = info.HowTo;
                    game.DateAdded = info.DateAdded;
                    game.Description = $"{info.Description} \n {info.DateAdded}";
                    game.LeaderBoard = info.LeaderBoard;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving game information from API.");
        }

        return games;
    }



    //Takes the user to the deprecated snake page
    public IActionResult Snake()
    {
        return View();
    }

    //Takes the user to the deprecated tetris page
    public IActionResult Tetris()
    {
        return View();
    }
}
