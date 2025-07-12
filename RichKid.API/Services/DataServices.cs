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
        private readonly ILogger<DataService> _logger; // Add logger for tracking file operations

        public DataService(ILogger<DataService> logger)
        {
            // Calculate the path to Users.json file (same logic as before)
            var basePath = AppContext.BaseDirectory;
            var solutionPath = Path.GetFullPath(Path.Combine(basePath, "../../../../"));
            _filePath = Path.Combine(solutionPath, "Users.json");
            
            _logger = logger; // Inject logger to monitor file I/O operations
            
            // Log the file path being used for data storage
            _logger.LogInformation("DataService initialized with file path: {FilePath}", _filePath);
            
            // Check if the file exists and log its status
            if (File.Exists(_filePath))
            {
                _logger.LogDebug("Users data file exists and is accessible");
            }
            else
            {
                _logger.LogWarning("Users data file does not exist at: {FilePath}. Will be created on first save.", _filePath);
            }
        }

        public List<User> LoadUsers()
        {
            try
            {
                _logger.LogDebug("Starting to load users from file: {FilePath}", _filePath);
                
                // Return empty list if file doesn't exist (first run scenario)
                if (!File.Exists(_filePath))
                {
                    _logger.LogInformation("Users file not found, returning empty user list. File will be created on first save.");
                    return new List<User>();
                }

                // Read the JSON file content
                _logger.LogDebug("Reading JSON content from users file");
                var json = File.ReadAllText(_filePath);
                
                if (string.IsNullOrWhiteSpace(json))
                {
                    _logger.LogWarning("Users file is empty, returning empty user list");
                    return new List<User>();
                }

                _logger.LogDebug("JSON content loaded, length: {JsonLength} characters", json.Length);

                // Parse the JSON and extract the Users array
                using var doc = JsonDocument.Parse(json);
                
                if (!doc.RootElement.TryGetProperty("Users", out var usersElement))
                {
                    _logger.LogWarning("JSON file does not contain 'Users' property, returning empty list");
                    return new List<User>();
                }

                var users = usersElement.Deserialize<List<User>>();
                var userCount = users?.Count ?? 0;
                
                _logger.LogInformation("Successfully loaded {UserCount} users from data file", userCount);
                
                // Log some basic statistics for monitoring
                if (users != null && users.Count > 0)
                {
                    var activeUsers = users.Count(u => u.Active);
                    var inactiveUsers = users.Count - activeUsers;
                    _logger.LogDebug("User statistics - Active: {ActiveCount}, Inactive: {InactiveCount}", 
                        activeUsers, inactiveUsers);
                }
                
                return users ?? new List<User>();
            }
            catch (JsonException ex)
            {
                // Handle JSON parsing errors specifically
                _logger.LogError(ex, "Failed to parse JSON from users file: {FilePath}. The file may be corrupted.", _filePath);
                throw new InvalidOperationException($"The users data file contains invalid JSON: {ex.Message}", ex);
            }
            catch (UnauthorizedAccessException ex)
            {
                // Handle file permission issues
                _logger.LogError(ex, "Access denied when trying to read users file: {FilePath}", _filePath);
                throw new InvalidOperationException($"Cannot access the users data file. Check file permissions: {ex.Message}", ex);
            }
            catch (IOException ex)
            {
                // Handle other I/O related errors
                _logger.LogError(ex, "I/O error occurred while reading users file: {FilePath}", _filePath);
                throw new InvalidOperationException($"Error reading the users data file: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                // Handle any other unexpected errors
                _logger.LogError(ex, "Unexpected error occurred while loading users from: {FilePath}", _filePath);
                throw new InvalidOperationException($"Unexpected error loading users: {ex.Message}", ex);
            }
        }

        public void SaveUsers(List<User> users)
        {
            try
            {
                var userCount = users?.Count ?? 0;
                _logger.LogInformation("Starting to save {UserCount} users to file: {FilePath}", userCount, _filePath);
                
                if (users == null)
                {
                    _logger.LogWarning("Attempting to save null users list, will save empty list instead");
                    users = new List<User>();
                }

                // Wrap users in an object structure for JSON (maintains existing format)
                var wrapper = new { Users = users };
                
                // Serialize to JSON with readable formatting
                var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(wrapper, jsonOptions);
                
                _logger.LogDebug("JSON serialized successfully, length: {JsonLength} characters", json.Length);

                // Ensure the directory exists before writing
                var directory = Path.GetDirectoryName(_filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    _logger.LogDebug("Creating directory: {Directory}", directory);
                    Directory.CreateDirectory(directory);
                }

                // Write JSON to file
                File.WriteAllText(_filePath, json);
                
                _logger.LogInformation("Successfully saved {UserCount} users to data file", userCount);
                
                // Log some statistics about what was saved
                if (users.Count > 0)
                {
                    var activeUsers = users.Count(u => u.Active);
                    var inactiveUsers = users.Count - activeUsers;
                    var userGroups = users.GroupBy(u => u.UserGroupID ?? 0)
                                          .Select(g => new { GroupId = g.Key, Count = g.Count() })
                                          .ToList();
                    
                    _logger.LogDebug("Saved user statistics - Active: {ActiveCount}, Inactive: {InactiveCount}", 
                        activeUsers, inactiveUsers);
                    
                    foreach (var group in userGroups)
                    {
                        var groupName = UserGroups.GetName(group.GroupId, useEnglish: true);
                        _logger.LogDebug("Group {GroupName} (ID: {GroupId}): {UserCount} users", 
                            groupName, group.GroupId, group.Count);
                    }
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                // Handle file permission issues
                _logger.LogError(ex, "Access denied when trying to save users to file: {FilePath}", _filePath);
                throw new InvalidOperationException($"Cannot save to the users data file. Check file permissions: {ex.Message}", ex);
            }
            catch (DirectoryNotFoundException ex)
            {
                // Handle missing directory issues
                _logger.LogError(ex, "Directory not found for users file: {FilePath}", _filePath);
                throw new InvalidOperationException($"Cannot find directory for users data file: {ex.Message}", ex);
            }
            catch (IOException ex)
            {
                // Handle other I/O related errors
                _logger.LogError(ex, "I/O error occurred while saving users to file: {FilePath}", _filePath);
                throw new InvalidOperationException($"Error writing to the users data file: {ex.Message}", ex);
            }
            catch (JsonException ex)
            {
                // Handle JSON serialization errors
                _logger.LogError(ex, "Failed to serialize users to JSON for file: {FilePath}", _filePath);
                throw new InvalidOperationException($"Error converting users to JSON format: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                // Handle any other unexpected errors
                _logger.LogError(ex, "Unexpected error occurred while saving users to: {FilePath}", _filePath);
                throw new InvalidOperationException($"Unexpected error saving users: {ex.Message}", ex);
            }
        }
    }
}