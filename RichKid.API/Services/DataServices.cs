using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using RichKid.Shared.Models;
using RichKid.Shared.Services;

namespace RichKid.API.Services
{
    public class DataService : IDataService
    {
        private readonly string _filePath;

        public DataService()
        {
            var basePath = AppContext.BaseDirectory;
            // Navigate from BIN folder to solution root
            var solutionPath = Path.GetFullPath(Path.Combine(basePath, "../../../../"));
            _filePath = Path.Combine(solutionPath, "Users.json");
        }

        public List<User> LoadUsers()
        {
            // Return empty list if file doesn't exist
            if (!File.Exists(_filePath))
                return new List<User>();

            // Read and deserialize JSON file
            var json = File.ReadAllText(_filePath);
            using var doc = JsonDocument.Parse(json);
            var users = doc.RootElement
                           .GetProperty("Users")
                           .Deserialize<List<User>>();
            return users ?? new List<User>();
        }

        public void SaveUsers(List<User> users)
        {
            // Wrap users in an object structure for JSON
            var wrapper = new { Users = users };
            var json = JsonSerializer.Serialize(wrapper,
                new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_filePath, json);
        }
    }
}