using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using BucStop.Models;
using Microsoft.AspNetCore.Hosting;

namespace BucStop.Services
{
    public class SnapshotService
    {
        private readonly string _snapshotsDirectory;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public SnapshotService(IWebHostEnvironment webHostEnvironment)
        {
            _webHostEnvironment = webHostEnvironment;
            _snapshotsDirectory = Path.Combine(_webHostEnvironment.ContentRootPath, "Snapshots");
            
            // Ensure directory exists
            if (!Directory.Exists(_snapshotsDirectory))
            {
                Directory.CreateDirectory(_snapshotsDirectory);
            }
        }

        public async Task SaveSnapshotAsync(Snapshot snapshot)
        {
            var filePath = Path.Combine(_snapshotsDirectory, $"{snapshot.Id}.json");
            var json = JsonSerializer.Serialize(snapshot, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            await File.WriteAllTextAsync(filePath, json);
        }

        public async Task<Snapshot> GetSnapshotAsync(string id)
        {
            var filePath = Path.Combine(_snapshotsDirectory, $"{id}.json");
            if (!File.Exists(filePath))
                return null;

            var json = await File.ReadAllTextAsync(filePath);
            return JsonSerializer.Deserialize<Snapshot>(json);
        }

        public async Task<List<Snapshot>> GetAllSnapshotsAsync()
        {
            var snapshots = new List<Snapshot>();
            if (!Directory.Exists(_snapshotsDirectory))
            {
                return snapshots;
            }

            foreach (var file in Directory.GetFiles(_snapshotsDirectory, "*.json"))
            {
                var json = await File.ReadAllTextAsync(file);
                var snapshot = JsonSerializer.Deserialize<Snapshot>(json);
                if (snapshot != null)
                {
                    snapshots.Add(snapshot);
                }
            }
            
            // Sort snapshots by timestamp descending (newest first)
            snapshots.Sort((a, b) => b.Timestamp.CompareTo(a.Timestamp));
            return snapshots;
        }
    }
} 