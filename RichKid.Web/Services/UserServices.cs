using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using RichKid.Web.Models;

namespace RichKid.Web.Services
{
    public class UserService
    {
        private readonly string _filePath;

        public UserService()
        {
            var basePath = AppContext.BaseDirectory;
            var solutionPath = Path.GetFullPath(Path.Combine(basePath, "../../../../")); // קפיצה מה-BIN אל שורש ה-sln
            _filePath = Path.Combine(solutionPath, "Users.json");

            // Debug – וודא שהנתיב נכון ושהקובץ אכן קיים
            Console.WriteLine($"[DEBUG] Users.json path: {_filePath}");
            Console.WriteLine($"[DEBUG] Exists: {File.Exists(_filePath)}");
        }

        public List<User> GetAllUsers()
        {
            if (!File.Exists(_filePath))
                return new List<User>();

            var json = File.ReadAllText(_filePath);
            using var doc = JsonDocument.Parse(json);
            var users = doc.RootElement
                           .GetProperty("Users")
                           .Deserialize<List<User>>();
            return users ?? new List<User>();
        }

        public void SaveAllUsers(List<User> users)
        {
            var wrapper = new { Users = users };
            var json = JsonSerializer.Serialize(wrapper,
                new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_filePath, json);
        }

        public void AddUser(User user)
        {
            var users = GetAllUsers();
            if (users.Any(u => u.UserName == user.UserName))
                throw new Exception("שם המשתמש כבר קיים במערכת.");

            user.UserID = users.Any() ? users.Max(u => u.UserID) + 1 : 1;

            if (user.Data == null)
                user.Data = new UserData();

            user.Data.CreationDate = DateTime.Now.ToString("yyyy-MM-dd");
            users.Add(user);
            SaveAllUsers(users);
        }

        public void DeleteUser(int id)
        {
            var users = GetAllUsers();
            var toRemove = users.FirstOrDefault(u => u.UserID == id);
            if (toRemove != null)
            {
                users.Remove(toRemove);
                SaveAllUsers(users);
            }
        }

        public void UpdateUser(User updatedUser)
        {
            var users = GetAllUsers();

            if (users.Any(u => u.UserName == updatedUser.UserName && u.UserID != updatedUser.UserID))
                throw new Exception("שם המשתמש כבר קיים במערכת.");

            var existing = users.FirstOrDefault(u => u.UserID == updatedUser.UserID);
            if (existing != null)
            {
                existing.UserName    = updatedUser.UserName;
                existing.Password    = updatedUser.Password;
                existing.Active      = updatedUser.Active;
                existing.UserGroupID = updatedUser.UserGroupID;
                existing.Data        = updatedUser.Data;

                SaveAllUsers(users);
            }
        }

        public User? GetUserById(int id)
            => GetAllUsers().FirstOrDefault(u => u.UserID == id);
    }
}
