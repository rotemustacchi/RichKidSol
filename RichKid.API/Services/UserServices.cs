using System;
using System.Collections.Generic;
using System.Linq;
using RichKid.Shared.Models;
using RichKid.Shared.Services;

namespace RichKid.API.Services
{
    public class UserService : IUserService
    {
        private readonly IDataService _dataService;
        private readonly ILogger<UserService> _logger; // Add logger for tracking data operations

        public UserService(IDataService dataService, ILogger<UserService> logger)
        {
            _dataService = dataService;
            _logger = logger; // Inject logger to monitor all user data operations
            
            _logger.LogDebug("UserService initialized successfully");
        }

        public List<User> GetAllUsers()
        {
            try
            {
                _logger.LogDebug("Starting to retrieve all users from data service");
                
                // Load users from the data service
                var users = _dataService.LoadUsers();
                
                _logger.LogInformation("Successfully retrieved {UserCount} users from data storage", users.Count);
                
                return users;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve users from data service");
                throw; // Re-throw to let caller handle the error
            }
        }

        public void AddUser(User user)
        {
            try
            {
                _logger.LogInformation("Starting to add new user: {UserName}", user.UserName);
                
                // Load existing users to check for conflicts and generate ID
                var users = GetAllUsers();
                
                // Check if username already exists in the system
                if (users.Any(u => u.UserName == user.UserName))
                {
                    _logger.LogWarning("Add user failed - Username already exists: {UserName}", user.UserName);
                    throw new Exception("Username already exists in the system.");
                }

                // Auto-generate user ID based on highest existing ID
                var newUserId = users.Any() ? users.Max(u => u.UserID) + 1 : 1;
                user.UserID = newUserId;
                
                _logger.LogDebug("Assigned new user ID: {UserId} to user: {UserName}", newUserId, user.UserName);

                // Initialize user data if not provided
                if (user.Data == null)
                {
                    user.Data = new UserData();
                    _logger.LogDebug("Initialized empty user data for user: {UserName}", user.UserName);
                }

                // Set creation date as string in YYYY-MM-DD format
                user.Data.CreationDate = DateTime.Now.ToString("yyyy-MM-dd");
                _logger.LogDebug("Set creation date to {CreationDate} for user: {UserName}", 
                    user.Data.CreationDate, user.UserName);
                
                // Add user to the list and save to storage
                users.Add(user);
                _dataService.SaveUsers(users);
                
                _logger.LogInformation("Successfully added new user: {UserName} (ID: {UserId}) with group: {GroupId}", 
                    user.UserName, user.UserID, user.UserGroupID);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add user: {UserName}", user.UserName);
                throw; // Re-throw to let caller handle the error
            }
        }

        public void DeleteUser(int id)
        {
            try
            {
                _logger.LogInformation("Starting to delete user with ID: {UserId}", id);
                
                // Load current users list
                var users = GetAllUsers();
                var toRemove = users.FirstOrDefault(u => u.UserID == id);
                
                if (toRemove != null)
                {
                    var userNameToDelete = toRemove.UserName;
                    
                    // Remove the user from the list
                    users.Remove(toRemove);
                    _dataService.SaveUsers(users);
                    
                    _logger.LogInformation("Successfully deleted user: {UserName} (ID: {UserId})", 
                        userNameToDelete, id);
                }
                else
                {
                    _logger.LogWarning("Delete user failed - User not found with ID: {UserId}", id);
                    // Note: This follows the original logic of silently handling non-existent users
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete user with ID: {UserId}", id);
                throw; // Re-throw to let caller handle the error
            }
        }

        public void UpdateUser(User updatedUser)
        {
            try
            {
                _logger.LogInformation("Starting to update user: {UserName} (ID: {UserId})", 
                    updatedUser.UserName, updatedUser.UserID);
                
                // Load current users list
                var users = GetAllUsers();

                // Check if username is already taken by another user
                var existingUserWithSameName = users.FirstOrDefault(u => 
                    u.UserName == updatedUser.UserName && u.UserID != updatedUser.UserID);
                    
                if (existingUserWithSameName != null)
                {
                    _logger.LogWarning("Update user failed - Username '{UserName}' already exists for user ID: {ExistingUserId}, cannot assign to user ID: {TargetUserId}", 
                        updatedUser.UserName, existingUserWithSameName.UserID, updatedUser.UserID);
                    throw new Exception("Username already exists in the system.");
                }

                // Find the existing user to update
                var existing = users.FirstOrDefault(u => u.UserID == updatedUser.UserID);
                if (existing != null)
                {
                    var oldUserName = existing.UserName;
                    
                    // Update all user properties
                    existing.UserName    = updatedUser.UserName;
                    existing.Password    = updatedUser.Password;
                    existing.Active      = updatedUser.Active;
                    existing.UserGroupID = updatedUser.UserGroupID;
                    existing.Data        = updatedUser.Data;

                    // Save the updated users list
                    _dataService.SaveUsers(users);
                    
                    _logger.LogInformation("Successfully updated user: {OldUserName} -> {NewUserName} (ID: {UserId}), Active: {IsActive}, Group: {GroupId}", 
                        oldUserName, updatedUser.UserName, updatedUser.UserID, updatedUser.Active, updatedUser.UserGroupID);
                }
                else
                {
                    _logger.LogWarning("Update user failed - User not found with ID: {UserId}", updatedUser.UserID);
                    // Note: Following original logic - silently handle non-existent users
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update user: {UserName} (ID: {UserId})", 
                    updatedUser.UserName, updatedUser.UserID);
                throw; // Re-throw to let caller handle the error
            }
        }

        public User? GetUserById(int id)
        {
            try
            {
                _logger.LogDebug("Looking up user by ID: {UserId}", id);
                
                // Find user in the loaded users list
                var user = GetAllUsers().FirstOrDefault(u => u.UserID == id);
                
                if (user != null)
                {
                    _logger.LogDebug("Found user: {UserName} (ID: {UserId})", user.UserName, id);
                }
                else
                {
                    _logger.LogDebug("User not found with ID: {UserId}", id);
                }
                
                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get user by ID: {UserId}", id);
                throw; // Re-throw to let caller handle the error
            }
        }

        public List<User> SearchByFullName(string first, string last)
        {
            try
            {
                _logger.LogInformation("Starting user search with FirstName: '{FirstName}', LastName: '{LastName}'", 
                    first ?? "null", last ?? "null");
                
                // Perform case-insensitive search on first and last names
                var results = GetAllUsers()
                    .Where(u =>
                        u.Data.FirstName.Contains(first, StringComparison.OrdinalIgnoreCase) &&
                        u.Data.LastName.Contains(last, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                
                _logger.LogInformation("Search completed. Found {ResultCount} users matching search criteria", results.Count);
                
                // Log the found usernames for debugging purposes
                if (results.Count > 0)
                {
                    var foundUserNames = string.Join(", ", results.Select(u => u.UserName));
                    _logger.LogDebug("Found users: {FoundUserNames}", foundUserNames);
                }
                
                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to search users by name - FirstName: '{FirstName}', LastName: '{LastName}'", 
                    first, last);
                throw; // Re-throw to let caller handle the error
            }
        }
    }
}