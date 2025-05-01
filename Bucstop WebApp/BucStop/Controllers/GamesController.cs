﻿using BucStop.Models;
using BucStop.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using System.Diagnostics;

/*
 * This file handles the links to each of the game pages.
 */

namespace BucStop.Controllers
{
    [Authorize]
    public class GamesController : Controller
    {
        private readonly MicroClient _httpClient;
        private readonly PlayCountManager _playCountManager;
        private readonly ILogger<GamesController> _logger;

        public GamesController(MicroClient microClient, IWebHostEnvironment webHostEnvironment, ILogger<GamesController> logger)
        {
            _httpClient = microClient;
            _logger = logger;

            // Initialize the PlayCountManager with the web root path and the JSON file name
            _playCountManager = new PlayCountManager(_httpClient.GetGamesList() ?? new List<Game>(), webHostEnvironment);
        }

        //Takes the user to the index page, passing the games list as an argument
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> IndexAsync()
        {
            _logger.LogInformation("Games index page accessed.");
           
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            //await the async gamesinfo
            List<Game> games = _httpClient.GetGamesList();

            //have to update playcounts here since the we are reading it dynamically now instead of from a static list
            foreach (Game game in games)
            {
                game.PlayCount = _playCountManager.GetPlayCount(game.Id);
            }

            // Sort games by their Id so that they always appear in order on the games page
            // Sorting here means that every refresh will re-sort the games but allows sorting by PlayCount if needed.
            games.Sort((x, y) => x.Id.CompareTo(y.Id));

            stopwatch.Stop();

            _logger.LogInformation("{Category}: Games Page Loaded in {LoadTime}ms.", "PageLoadTimes", stopwatch.ElapsedMilliseconds);
            _logger.LogInformation("{Category}: {User} accessed the games index page.", "UserActivity", User.Identity?.Name ?? "Anonymous");

            return View(games);
        }

        //Takes the user to the Play page, passing the game object the user wants to play
        public async Task<IActionResult> Play(int id)
        {
            _logger.LogInformation("{Category}: User requested to play game with ID {GameId}.", "GameSuccess", id);

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            //await the async gamesinfo
            List<Game> games = _httpClient.GetGamesList();

            Game game = games.FirstOrDefault(x => x.Id == id);
            if (game == null)
            {
                _logger.LogWarning("{Category}: Game with ID {GameId} not found.", "GameSuccess", id);
                return NotFound();
            }
            // Log the URL of the game being loaded
            _logger.LogInformation("Loading game URL: {GameUrl}", game.Content);
            // Increment the play count for the game with the specified ID
            _playCountManager.IncrementPlayCount(id);

            int playCount = _playCountManager.GetPlayCount(id);

            // Update the game's play count
            game.PlayCount = playCount;

            _logger.LogInformation("{Category}: Game '{GameTitle}' (ID: {GameId}) successfully loaded.",
                                    "GameSuccess", game.Title, game.Id);
            _logger.LogInformation("{Category}: {User} started playing '{GameTitle}' (ID: {GameId}).",
                                    "UserActivity", User.Identity?.Name ?? "Anonymous", game.Title, game.Id);

            stopwatch.Stop();

            _logger.LogInformation("{Category}: {GameTitle} Page Loaded in {LoadTime}ms.", "PageLoadTimes", game.Title, stopwatch.ElapsedMilliseconds);

            return View(game);
        }


        public async Task<List<Game>> GetGamesWithInfo()
        {

            List<Game> games = new List<Game>();

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
}