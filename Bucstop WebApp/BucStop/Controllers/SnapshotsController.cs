using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using BucStop.Models;
using BucStop.Services;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using Microsoft.Extensions.Logging;

namespace BucStop.Controllers
{
    public class SnapshotsController : Controller
    {
        private readonly SnapshotService _snapshotService;
        private readonly PlayCountManager _playCountManager;
        private readonly GameService _gameService;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ILogger<SnapshotsController> _logger;

        public SnapshotsController(
            SnapshotService snapshotService,
            IWebHostEnvironment webHostEnvironment,
            GameService gameService,
            ILogger<SnapshotsController> logger)
        {
            _snapshotService = snapshotService;
            _gameService = gameService;
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
            _playCountManager = new PlayCountManager(_gameService.GetGames() ?? new List<Game>(), webHostEnvironment);
        }

        public async Task<IActionResult> Index()
        {
            var snapshots = await _snapshotService.GetAllSnapshotsAsync();
            return View(snapshots);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(string description)
        {
            var snapshot = new Snapshot
            {
                Timestamp = DateTime.UtcNow,
                Description = description
            };

            // Get games and their play counts
            var games = _gameService.GetGames();
            if (games != null)
            {
                snapshot.PlayerCounts["Snake"] = _playCountManager.GetPlayCount(1);  // Snake has ID 1
                snapshot.PlayerCounts["Tetris"] = _playCountManager.GetPlayCount(2); // Tetris has ID 2
                snapshot.PlayerCounts["Pong"] = _playCountManager.GetPlayCount(3);   // Pong has ID 3
            }

            // Add logs from the log files
            var logsDirectory = Path.Combine(_webHostEnvironment.ContentRootPath, "Logs");
            if (Directory.Exists(logsDirectory))
            {
                var pageLoadLogPath = Path.Combine(logsDirectory, "page_load_times.log");
                var userActivityLogPath = Path.Combine(logsDirectory, "user_activity.log");
                var gameSuccessLogPath = Path.Combine(logsDirectory, "game_success.log");
                var apiHeartbeatLogPath = Path.Combine(logsDirectory, "api_heartbeat.log");

                if (System.IO.File.Exists(pageLoadLogPath))
                    snapshot.Logs["page_load"] = await System.IO.File.ReadAllTextAsync(pageLoadLogPath);
                
                if (System.IO.File.Exists(userActivityLogPath))
                    snapshot.Logs["user_activity"] = await System.IO.File.ReadAllTextAsync(userActivityLogPath);
                
                if (System.IO.File.Exists(gameSuccessLogPath))
                    snapshot.Logs["game_success"] = await System.IO.File.ReadAllTextAsync(gameSuccessLogPath);
                
                if (System.IO.File.Exists(apiHeartbeatLogPath))
                    snapshot.Logs["api_heartbeat"] = await System.IO.File.ReadAllTextAsync(apiHeartbeatLogPath);
            }

            await _snapshotService.SaveSnapshotAsync(snapshot);
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Details(string id)
        {
            var snapshot = await _snapshotService.GetSnapshotAsync(id);
            if (snapshot == null)
                return NotFound();

            return View(snapshot);
        }

        [HttpPost]
        public async Task<IActionResult> Rollback(string id)
        {
            var snapshot = await _snapshotService.GetSnapshotAsync(id);
            if (snapshot == null)
                return NotFound();

            try
            {
                await _playCountManager.RollbackToSnapshot(snapshot.PlayerCounts);
                _logger.LogInformation("Successfully rolled back to snapshot {Id} from {Timestamp}", 
                    snapshot.Id, snapshot.Timestamp);
                TempData["SuccessMessage"] = $"Successfully rolled back to snapshot from {snapshot.Timestamp:yyyy-MM-dd HH:mm:ss}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to rollback to snapshot {Id}", snapshot.Id);
                TempData["ErrorMessage"] = "Failed to rollback to snapshot. Please try again.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
} 