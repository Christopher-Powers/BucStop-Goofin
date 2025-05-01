using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using BucStop.Models;
using BucStop.Services;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Net.Http;


namespace BucStop.Controllers
{
    public class SnapshotsController : Controller
    {
        private readonly SnapshotService _snapshotService;
        private readonly PlayCountManager _playCountManager;
        private readonly MicroClient _httpClient;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ILogger<SnapshotsController> _logger;
        private readonly IHostEnvironment _host;

        public SnapshotsController(
            SnapshotService snapshotService,
            IWebHostEnvironment webHostEnvironment,
            MicroClient microClient,
            ILogger<SnapshotsController> logger,
            IHostEnvironment host)
        {
            _snapshotService = snapshotService;
            _httpClient = microClient;
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
            _playCountManager = new PlayCountManager(_httpClient.GetGamesList() ?? new List<Game>(), webHostEnvironment);
            _host = host;
        }

        public async Task<IActionResult> Index()
        {
            var snapshots = await _snapshotService.GetAllSnapshotsAsync();
            if (_host.IsEnvironment("containers") || _host.IsEnvironment("containersLocal"))
            {
                var gitHash = Environment.GetEnvironmentVariable("GIT_COMMIT_HASH") ?? "unknown";
                ViewBag.GitHash = gitHash;
            }
            else if (_host.IsDevelopment())
            {
                ViewBag.GitHash = GetGitCommitHash();
            }
            else
            {
                _logger.LogInformation("Unable to get Git Commit Hash info. Environment variable cannot be detected.");
            }
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

            // Add Git commit hash info here
            if (_host.IsEnvironment("containers") || _host.IsEnvironment("containersLocal"))
            {
                var gitHash = Environment.GetEnvironmentVariable("GIT_COMMIT_HASH") ?? "unknown";
                snapshot.GitCommit = gitHash;
                _logger.LogInformation("Current Git Commit Hash is {CommitHash}", snapshot.GitCommit);
            }
            else if (_host.IsDevelopment())
            {
                snapshot.GitCommit = GetGitCommitHash();
                _logger.LogInformation("Current Git Commit Hash is {CommitHash}", snapshot.GitCommit);
            }
            else
            {
                _logger.LogInformation("Unable to get Git Commit Hash info. Environment variable cannot be detected.");
            }

            
            // Get games and their play counts
            var games = _httpClient.GetGamesList();
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
                var pageLoadLogPaths = Directory.GetFiles(logsDirectory, "page_load_times*"); 
                var userActivityLogPaths = Directory.GetFiles(logsDirectory, "user_activity*");
                var gameSuccessLogPaths = Directory.GetFiles(logsDirectory, "game_success*");
                var apiHeartbeatLogPaths = Directory.GetFiles(logsDirectory, "api_heartbeat*");

                foreach (var path in pageLoadLogPaths)
                {
                    try
                    {
                        using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        using (var reader = new StreamReader(stream))
                        {
                            var content = await reader.ReadToEndAsync();
                            snapshot.Logs[$"page_load:{Path.GetFileName(path)}"] = content;
                        }
                    }
                    catch (IOException ex)
                    {
                        Console.WriteLine($"Could not read {path}: {ex.Message}");
                    }
                }

                foreach (var path in userActivityLogPaths)
                {
                    try
                    {
                        using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        using (var reader = new StreamReader(stream))
                        {
                            var content = await reader.ReadToEndAsync();
                            snapshot.Logs[$"user_activity:{Path.GetFileName(path)}"] = content;
                        }
                    }
                    catch (IOException ex)
                    {
                        Console.WriteLine($"Could not read {path}: {ex.Message}");
                    }
                }

                foreach (var path in gameSuccessLogPaths)
                {
                    try
                    {
                        using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        using (var reader = new StreamReader(stream))
                        {
                            var content = await reader.ReadToEndAsync();
                            snapshot.Logs[$"game_success:{Path.GetFileName(path)}"] = content;
                        }
                    }
                    catch (IOException ex)
                    {
                        Console.WriteLine($"Could not read {path}: {ex.Message}");
                    }
                }

                foreach (var path in apiHeartbeatLogPaths)
                {
                    try
                    {
                        using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        using (var reader = new StreamReader(stream))
                        {
                            var content = await reader.ReadToEndAsync();
                            snapshot.Logs[$"api_heartbeat:{Path.GetFileName(path)}"] = content;
                        }
                    }
                    catch (IOException ex)
                    {
                        Console.WriteLine($"Could not read {path}: {ex.Message}");
                    }
                }
            }

            await _snapshotService.SaveSnapshotAsync(snapshot);
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Details(string id)
        {
            var snapshot = await _snapshotService.GetSnapshotAsync(id);
            if (snapshot == null)
                return NotFound();

            if (_host.IsEnvironment("containers") || _host.IsEnvironment("containersLocal"))
            {
                var gitHash = Environment.GetEnvironmentVariable("GIT_COMMIT_HASH") ?? "unknown";
                ViewBag.GitHash = gitHash;
            }
            else if (_host.IsDevelopment())
            {
                ViewBag.GitHash = GetGitCommitHash();
            }
            else
            {
                _logger.LogInformation("Unable to get Git Commit Hash info. Environment variable cannot be detected.");
            }
            return View(snapshot);
        }

        [HttpPost]
        public async Task<IActionResult> Rollback(string id)
        {
            var snapshot = await _snapshotService.GetSnapshotAsync(id);
            if (snapshot == null)
                return NotFound();

            //probably have a check here if the current git commit hash does not match the snapshot.gitcommit info?
            string currentGitHash = "N/A";
            if (_host.IsEnvironment("containers") || _host.IsEnvironment("containersLocal"))
            {
                var gitHash = Environment.GetEnvironmentVariable("GIT_COMMIT_HASH") ?? "unknown";
                currentGitHash = gitHash;
            }
            else if (_host.IsDevelopment())
            {
                currentGitHash = GetGitCommitHash();
            }
            else
            {
                _logger.LogInformation("Unable to get Git Commit Hash info. Environment variable cannot be detected.");
            }

            if (snapshot.GitCommit != currentGitHash)
            {
                _logger.LogInformation("Git Commit Hash does not match that of the Snapshot. \nSnapshot: {SnapshotHash} \nCurrent: {CurrentHash}", snapshot.GitCommit, currentGitHash);
            }

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

        string GetGitCommitHash()
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = "rev-parse HEAD",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = new Process { StartInfo = startInfo })
            {
                process.Start();
                string output = process.StandardOutput.ReadToEnd().Trim();
                process.WaitForExit();
                return output;
            }
        }
    }
} 