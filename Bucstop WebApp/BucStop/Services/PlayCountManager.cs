﻿using BucStop.Models;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace BucStop.Services
{

    public class PlayCountManager
    {
        private List<Game> games;
        private string jsonFilePath;  // Field to store the JSON file path

        public PlayCountManager(List<Game> games, IWebHostEnvironment webHostEnvironment)
        {
            this.games = games;
            jsonFilePath = Path.Combine(webHostEnvironment.WebRootPath, "playcount.json");
            LoadPlayCounts();
        }


        public void IncrementPlayCount(int gameId)
        {
            var game = games.FirstOrDefault(g => g.Id == gameId);
            if (game != null)
            {
                game.PlayCount++;
                SavePlayCounts();
            }
        }

        public int GetPlayCount(int gameId)
        {
            var game = games.FirstOrDefault(g => g.Id == gameId);
            return game?.PlayCount ?? 0;
        }

        public void SetPlayCount(int gameId, int count)
        {
            var game = games.FirstOrDefault(g => g.Id == gameId);
            if (game != null)
            {
                game.PlayCount = count;
                SavePlayCounts();
            }
        }

        public async Task RollbackToSnapshot(Dictionary<string, int> playerCounts)
        {
            // Map game names to IDs
            var gameIdMap = new Dictionary<string, int>
            {
                { "Snake", 1 },
                { "Tetris", 2 },
                { "Pong", 3 }
            };

            foreach (var kvp in playerCounts)
            {
                if (gameIdMap.TryGetValue(kvp.Key, out int gameId))
                {
                    SetPlayCount(gameId, kvp.Value);
                }
            }
        }

        private void LoadPlayCounts()
        {
            if (File.Exists(jsonFilePath))
            {
                var jsonText = File.ReadAllText(jsonFilePath);
                var playCountData = JsonSerializer.Deserialize<List<Game>>(jsonText);

                foreach (var playCount in playCountData)
                {
                    var existingGame = games.FirstOrDefault(g => g.Id == playCount.Id);
                    if (existingGame != null)
                    {
                        existingGame.PlayCount = playCount.PlayCount;
                    }
                }
            }
            else
            {
                foreach (var game in games)
                {
                    game.PlayCount = 0;
                }

                SavePlayCounts();
            }
        }

        private void SavePlayCounts()
        {
            var jsonText = JsonSerializer.Serialize(games);
            File.WriteAllText(jsonFilePath, jsonText);
        }
    }
}