﻿using BucStop.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

/*
 * This file has the controllers for everything outside of the games
 * and game-related pages.
 */

namespace BucStop.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly GameService _gameService;

        public HomeController(ILogger<HomeController> logger, GameService games)
        {
            _logger = logger;
            _gameService = games;
        }

        //Sends the user to the deprecated Index page.
        public IActionResult Index()
        {
            return View(_gameService.GetGames());
        }

        //Takes the user to the admin page.
        public IActionResult Admin()
        {
            _logger.LogInformation("{Category}: {User} visited the Admin page.", "UserActivity", User.Identity?.Name ?? "Anonymous");
            return View();
        }

        //Takes the user to the about policy page.
        public IActionResult Privacy()
        {
            _logger.LogInformation("{Category}: {User} visited the Privacy page.", "UserActivity", User.Identity?.Name ?? "Anonymous");
            return View();
        }

        //Takes the user to the game criteria page.
        public IActionResult GameCriteria()
        {
            _logger.LogInformation("{Category}: {User} visited the Game Criteria page.", "UserActivity", User.Identity?.Name ?? "Anonymous");
            return View();
        }

        //Takes the user to version 2.1 page
        public IActionResult TwoDotOne()
        {
            return View();
        }

        public IActionResult TwoDotTwo()
        {
            return View();
        }

        public IActionResult TwoDotThree()
        {
            return View();
        }

        public IActionResult TwoDotFour()
        {
            return View();
        }

        //If something goes wrong, this will take the user to a page explaining the error.
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}