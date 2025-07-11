using System;
using System.Collections.Generic;
using System.Linq;
using RichKid.Shared.Models;

namespace RichKid.Shared.Services
{
    public interface IUserService
    {
        List<User> GetAllUsers();
        void AddUser(User user);
        void DeleteUser(int id);
        void UpdateUser(User updatedUser);
        User? GetUserById(int id);
        List<User> SearchByFullName(string first, string last);
    }

    public class UserService : IUserService
    {
        private readonly IDataService _dataService;

        public UserService(IDataService dataService)
        {
            _dataService = dataService;
        }

        public List<User> GetAllUsers()
        {
            return _dataService.LoadUsers();
        }

        public void AddUser(User user)
        {
            var users = GetAllUsers();
            
            // Check if username already exists
            if (users.Any(u => u.UserName == user.UserName))
                throw new Exception("Username already exists in the system.");

            // Auto-generate user ID
            user.UserID = users.Any() ? users.Max(u => u.UserID) + 1 : 1;

            // Initialize user data if not provided
            if (user.Data == null)
                user.Data = new UserData();

            // Set creation date
            user.Data.CreationDate = DateTime.Now.ToString("yyyy-MM-dd");
            
            // Add user and save
            users.Add(user);
            _dataService.SaveUsers(users);
        }

        public void DeleteUser(int id)
        {
            var users = GetAllUsers();
            var toRemove = users.FirstOrDefault(u => u.UserID == id);
            
            if (toRemove != null)
            {
                users.Remove(toRemove);
                _dataService.SaveUsers(users);
            }
        }

        public void UpdateUser(User updatedUser)
        {
            var users = GetAllUsers();

            // Check if username is already taken by another user
            if (users.Any(u => u.UserName == updatedUser.UserName && u.UserID != updatedUser.UserID))
                throw new Exception("Username already exists in the system.");

            // Find and update existing user
            var existing = users.FirstOrDefault(u => u.UserID == updatedUser.UserID);
            if (existing != null)
            {
                existing.UserName    = updatedUser.UserName;
                existing.Password    = updatedUser.Password;
                existing.Active      = updatedUser.Active;
                existing.UserGroupID = updatedUser.UserGroupID;
                existing.Data        = updatedUser.Data;

                _dataService.SaveUsers(users);
            }
        }

        public User? GetUserById(int id)
            => GetAllUsers().FirstOrDefault(u => u.UserID == id);

        public List<User> SearchByFullName(string first, string last)
        {
            return GetAllUsers()
                .Where(u =>
                    u.Data.FirstName.Contains(first, StringComparison.OrdinalIgnoreCase) &&
                    u.Data.LastName.Contains(last, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
    }
}