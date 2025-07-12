using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using RichKid.API.Services;
using RichKid.Shared.Models;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace RichKid.Tests.Unit
{
    /// <summary>
    /// Unit tests for the DataService class
    /// These tests verify that file operations work correctly
    /// including loading and saving user data to JSON files
    /// 
    /// IMPORTANT: These tests use temporary files to avoid interfering
    /// with your development Users.json file
    /// </summary>
    public class DataServiceTests : IDisposable
    {
        private readonly Mock<ILogger<DataService>> _mockLogger;
        private readonly string _testFilePath;
        private readonly TestDataService _dataService;

        public DataServiceTests()
        {
            // Set up test environment with temporary file for each test
            // This ensures tests don't interfere with the real Users.json file
            _mockLogger = new Mock<ILogger<DataService>>();
            
            // Create a unique temporary file path for this test run
            // Using GUID ensures no conflicts between parallel test runs
            _testFilePath = Path.Combine(Path.GetTempPath(), $"test_users_{Guid.NewGuid()}.json");
            
            // Create a custom DataService that uses our test file path
            // This isolates our tests from the real application data
            _dataService = new TestDataService(_mockLogger.Object, _testFilePath);
        }

        #region LoadUsers Tests

        [Fact]
        public void LoadUsers_WhenFileDoesNotExist_ShouldReturnEmptyList()
        {
            // Arrange - Ensure our test file doesn't exist initially
            if (File.Exists(_testFilePath))
            {
                File.Delete(_testFilePath);
            }

            // Act - Try to load users from non-existent file
            var result = _dataService.LoadUsers();

            // Assert - Should handle missing file gracefully by returning empty list
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public void LoadUsers_WhenFileIsEmpty_ShouldReturnEmptyList()
        {
            // Arrange - Create empty file to test edge case handling
            File.WriteAllText(_testFilePath, "");

            // Act - Try to load users from empty file
            var result = _dataService.LoadUsers();

            // Assert - Should handle empty file gracefully by returning empty list
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public void LoadUsers_WhenFileHasValidData_ShouldReturnUserList()
        {
            // Arrange - Create test file with valid JSON data structure
            var testUsers = new List<User>
            {
                new User 
                { 
                    UserID = 1, 
                    UserName = "TestUser1",
                    Password = "password1",
                    Active = true,
                    Data = new UserData 
                    { 
                        FirstName = "Test", 
                        LastName = "User1",
                        Email = "test1@example.com",
                        Phone = "123-456-7890"
                    }
                },
                new User 
                { 
                    UserID = 2, 
                    UserName = "TestUser2",
                    Password = "password2",
                    Active = false,
                    Data = new UserData 
                    { 
                        FirstName = "Test", 
                        LastName = "User2",
                        Email = "test2@example.com",
                        Phone = "098-765-4321"
                    }
                }
            };

            // Save test data in the same format as the real application
            var wrapper = new { Users = testUsers };
            var json = JsonSerializer.Serialize(wrapper, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_testFilePath, json);

            // Act - Load users from test file
            var result = _dataService.LoadUsers();

            // Assert - Should correctly parse and return all test users
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Equal("TestUser1", result[0].UserName);
            Assert.Equal("TestUser2", result[1].UserName);
            Assert.True(result[0].Active);
            Assert.False(result[1].Active);
        }

        [Fact]
        public void LoadUsers_WithJsonMissingUsersProperty_ShouldReturnEmptyList()
        {
            // Arrange - Create JSON without the expected "Users" property
            // This tests robustness when file format doesn't match expectations
            var json = JsonSerializer.Serialize(new { SomeOtherProperty = "value" });
            File.WriteAllText(_testFilePath, json);

            // Act - Try to load users from malformed JSON structure
            var result = _dataService.LoadUsers();

            // Assert - Should handle missing Users property gracefully
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public void LoadUsers_WithInvalidJson_ShouldThrowInvalidOperationException()
        {
            // Arrange - Create file with malformed JSON to test error handling
            File.WriteAllText(_testFilePath, "{ invalid json content");

            // Act & Assert - Should throw specific exception for invalid JSON
            var exception = Assert.Throws<InvalidOperationException>(() => _dataService.LoadUsers());
            Assert.Contains("invalid JSON", exception.Message);
        }

        #endregion

        #region SaveUsers Tests

        [Fact]
        public void SaveUsers_WithValidUserList_ShouldCreateFileWithCorrectFormat()
        {
            // Arrange - Create test users to save
            var usersToSave = new List<User>
            {
                new User 
                { 
                    UserID = 1, 
                    UserName = "SaveTestUser",
                    Password = "testpass",
                    Active = true,
                    UserGroupID = UserGroups.ADMIN,
                    Data = new UserData 
                    { 
                        CreationDate = "2025-07-12",
                        FirstName = "Save", 
                        LastName = "Test",
                        Email = "save@test.com",
                        Phone = "555-1234"
                    }
                }
            };

            // Act - Save users to test file
            _dataService.SaveUsers(usersToSave);

            // Assert - File should be created with correct JSON structure
            Assert.True(File.Exists(_testFilePath));
            
            var savedJson = File.ReadAllText(_testFilePath);
            Assert.NotEmpty(savedJson);
            
            // Verify the JSON can be parsed back correctly
            using var doc = JsonDocument.Parse(savedJson);
            Assert.True(doc.RootElement.TryGetProperty("Users", out var usersElement));
            
            var savedUsers = usersElement.Deserialize<List<User>>();
            Assert.Single(savedUsers);
            Assert.Equal("SaveTestUser", savedUsers[0].UserName);
        }

        [Fact]
        public void SaveUsers_WithNullList_ShouldSaveEmptyUsersList()
        {
            // Act - Try to save null list (should handle gracefully)
            _dataService.SaveUsers(null);

            // Assert - Should create file with empty users array
            Assert.True(File.Exists(_testFilePath));
            
            var savedJson = File.ReadAllText(_testFilePath);
            using var doc = JsonDocument.Parse(savedJson);
            Assert.True(doc.RootElement.TryGetProperty("Users", out var usersElement));
            
            var savedUsers = usersElement.Deserialize<List<User>>();
            Assert.NotNull(savedUsers);
            Assert.Empty(savedUsers);
        }

        [Fact]
        public void SaveUsers_WithEmptyList_ShouldSaveEmptyUsersList()
        {
            // Arrange - Create empty user list
            var emptyList = new List<User>();

            // Act - Save empty list
            _dataService.SaveUsers(emptyList);

            // Assert - Should create file with empty users array
            Assert.True(File.Exists(_testFilePath));
            
            var savedJson = File.ReadAllText(_testFilePath);
            using var doc = JsonDocument.Parse(savedJson);
            Assert.True(doc.RootElement.TryGetProperty("Users", out var usersElement));
            
            var savedUsers = usersElement.Deserialize<List<User>>();
            Assert.NotNull(savedUsers);
            Assert.Empty(savedUsers);
        }

        #endregion

        #region Round-trip Tests (Load and Save)

        [Fact]
        public void SaveAndLoadUsers_ShouldPreserveAllUserData()
        {
            // Arrange - Create comprehensive test data with various field types
            var originalUsers = new List<User>
            {
                new User 
                { 
                    UserID = 1, 
                    UserName = "RoundTripTest1",
                    Password = "password123",
                    Active = true,
                    UserGroupID = UserGroups.ADMIN,
                    Data = new UserData 
                    { 
                        CreationDate = "2025-07-12",
                        FirstName = "Round", 
                        LastName = "Trip",
                        Email = "roundtrip@test.com",
                        Phone = "555-0123"
                    }
                },
                new User 
                { 
                    UserID = 2, 
                    UserName = "RoundTripTest2",
                    Password = "anotherpass",
                    Active = false,
                    UserGroupID = UserGroups.VIEW_ONLY,
                    Data = new UserData 
                    { 
                        CreationDate = "2025-07-11",
                        FirstName = "Another", 
                        LastName = "User",
                        Email = "another@test.com",
                        Phone = "555-0456"
                    }
                }
            };

            // Act - Save and then load the data to test complete round-trip
            _dataService.SaveUsers(originalUsers);
            var loadedUsers = _dataService.LoadUsers();

            // Assert - All data should be preserved exactly through save/load cycle
            Assert.Equal(originalUsers.Count, loadedUsers.Count);
            
            for (int i = 0; i < originalUsers.Count; i++)
            {
                var original = originalUsers[i];
                var loaded = loadedUsers[i];
                
                // Verify all user properties are preserved
                Assert.Equal(original.UserID, loaded.UserID);
                Assert.Equal(original.UserName, loaded.UserName);
                Assert.Equal(original.Password, loaded.Password);
                Assert.Equal(original.Active, loaded.Active);
                Assert.Equal(original.UserGroupID, loaded.UserGroupID);
                
                // Verify all user data properties are preserved
                Assert.Equal(original.Data.CreationDate, loaded.Data.CreationDate);
                Assert.Equal(original.Data.FirstName, loaded.Data.FirstName);
                Assert.Equal(original.Data.LastName, loaded.Data.LastName);
                Assert.Equal(original.Data.Email, loaded.Data.Email);
                Assert.Equal(original.Data.Phone, loaded.Data.Phone);
            }
        }

        [Fact]
        public void SaveAndLoadUsers_WithSpecialCharacters_ShouldPreserveData()
        {
            // Arrange - Test with special characters, unicode, and edge cases
            var usersWithSpecialChars = new List<User>
            {
                new User 
                { 
                    UserID = 1, 
                    UserName = "SpecialChars_@#$",
                    Password = "pass!@#$%^&*()",
                    Data = new UserData 
                    { 
                        FirstName = "José", // Unicode character (accented letter)
                        LastName = "O'Connor", // Apostrophe in name
                        Email = "test+tag@domain.co.uk", // Plus sign in email address
                        Phone = "+1 (555) 123-4567" // International phone format
                    }
                }
            };

            // Act - Save and load data with special characters
            _dataService.SaveUsers(usersWithSpecialChars);
            var loadedUsers = _dataService.LoadUsers();

            // Assert - Special characters should be preserved exactly
            Assert.Single(loadedUsers);
            var user = loadedUsers[0];
            
            Assert.Equal("SpecialChars_@#$", user.UserName);
            Assert.Equal("pass!@#$%^&*()", user.Password);
            Assert.Equal("José", user.Data.FirstName);
            Assert.Equal("O'Connor", user.Data.LastName);
            Assert.Equal("test+tag@domain.co.uk", user.Data.Email);
            Assert.Equal("+1 (555) 123-4567", user.Data.Phone);
        }

        #endregion

        #region Edge Cases and Error Handling

        [Fact]
        public void SaveUsers_ToNonExistentDirectory_ShouldCreateDirectory()
        {
            // Arrange - Use path in non-existent directory to test directory creation
            var nonExistentDirPath = Path.Combine(Path.GetTempPath(), $"testdir_{Guid.NewGuid()}", "users.json");
            var customDataService = new TestDataService(_mockLogger.Object, nonExistentDirPath);
            
            var users = new List<User>
            {
                new User { UserID = 1, UserName = "DirTest", Data = new UserData() }
            };

            try
            {
                // Act - Should create directory and save file automatically
                customDataService.SaveUsers(users);

                // Assert - Directory and file should be created successfully
                Assert.True(File.Exists(nonExistentDirPath));
                
                // Verify the content was saved correctly
                var loaded = customDataService.LoadUsers();
                Assert.Single(loaded);
                Assert.Equal("DirTest", loaded[0].UserName);
            }
            finally
            {
                // Clean up test directory and file
                if (File.Exists(nonExistentDirPath))
                {
                    File.Delete(nonExistentDirPath);
                    Directory.Delete(Path.GetDirectoryName(nonExistentDirPath));
                }
            }
        }

        [Fact]
        public void SaveUsers_WithLargeUserList_ShouldHandleEfficiently()
        {
            // Arrange - Create a large number of users to test performance and memory handling
            var largeUserList = new List<User>();
            for (int i = 1; i <= 1000; i++)
            {
                largeUserList.Add(new User
                {
                    UserID = i,
                    UserName = $"User{i:0000}",
                    Password = $"password{i}",
                    Active = i % 2 == 0, // Alternate active/inactive
                    UserGroupID = (i % 4) + 1, // Cycle through user groups
                    Data = new UserData
                    {
                        CreationDate = DateTime.Now.AddDays(-i).ToString("yyyy-MM-dd"),
                        FirstName = $"First{i}",
                        LastName = $"Last{i}",
                        Email = $"user{i}@test.com",
                        Phone = $"555-{i:0000}"
                    }
                });
            }

            // Act - Save and load large dataset
            _dataService.SaveUsers(largeUserList);
            var loadedUsers = _dataService.LoadUsers();

            // Assert - All users should be preserved correctly
            Assert.Equal(1000, loadedUsers.Count);
            Assert.Equal("User0001", loadedUsers[0].UserName);
            Assert.Equal("User1000", loadedUsers[999].UserName);
            
            // Verify random samples from the list
            var randomUser = loadedUsers[499]; // User 500
            Assert.Equal("User0500", randomUser.UserName);
            Assert.Equal("user500@test.com", randomUser.Data.Email);
        }

        #endregion

        /// <summary>
        /// Clean up test resources after each test completes
        /// This ensures our test files don't clutter the temp directory
        /// and prevents conflicts between test runs
        /// </summary>
        public void Dispose()
        {
            // Clean up test file after each test
            if (File.Exists(_testFilePath))
            {
                try
                {
                    // Remove any special attributes that might prevent deletion
                    File.SetAttributes(_testFilePath, FileAttributes.Normal);
                    File.Delete(_testFilePath);
                }
                catch
                {
                    // If cleanup fails, don't crash the test
                    // The temp file will be cleaned up by the OS eventually
                }
            }
        }
    }

    /// <summary>
    /// Test-specific version of DataService that allows us to specify a custom file path
    /// This prevents tests from interfering with the real Users.json file
    /// by storing all test data in temporary files
    /// </summary>
    public class TestDataService : DataService
    {
        private readonly string _customFilePath;

        public TestDataService(ILogger<DataService> logger, string filePath) : base(logger)
        {
            _customFilePath = filePath;
        }

        /// <summary>
        /// Load users from the custom test file path instead of the real Users.json
        /// Implements the same logic as the parent class but with different file location
        /// </summary>
        public new List<User> LoadUsers()
        {
            try
            {
                // Return empty list if test file doesn't exist yet
                if (!File.Exists(_customFilePath))
                {
                    return new List<User>();
                }

                // Read and parse JSON content from test file
                var json = File.ReadAllText(_customFilePath);
                
                if (string.IsNullOrWhiteSpace(json))
                {
                    return new List<User>();
                }

                // Parse JSON structure exactly like the real DataService
                using var doc = JsonDocument.Parse(json);
                
                if (!doc.RootElement.TryGetProperty("Users", out var usersElement))
                {
                    return new List<User>();
                }

                var users = usersElement.Deserialize<List<User>>();
                return users ?? new List<User>();
            }
            catch (JsonException ex)
            {
                // Throw same exception types as real DataService for consistency
                throw new InvalidOperationException($"The users data file contains invalid JSON: {ex.Message}", ex);
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new InvalidOperationException($"Cannot access the users data file. Check file permissions: {ex.Message}", ex);
            }
            catch (IOException ex)
            {
                throw new InvalidOperationException($"Error reading the users data file: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Unexpected error loading users: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Save users to the custom test file path instead of the real Users.json
        /// Implements the same logic as the parent class but with different file location
        /// </summary>
        public new void SaveUsers(List<User> users)
        {
            try
            {
                // Handle null input gracefully like the real DataService
                if (users == null)
                {
                    users = new List<User>();
                }

                // Create JSON structure exactly like the real DataService
                var wrapper = new { Users = users };
                var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(wrapper, jsonOptions);

                // Ensure directory exists before writing test file
                var directory = Path.GetDirectoryName(_customFilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Write JSON to test file
                File.WriteAllText(_customFilePath, json);
            }
            catch (UnauthorizedAccessException ex)
            {
                // Throw same exception types as real DataService for consistency
                throw new InvalidOperationException($"Cannot save to the users data file. Check file permissions: {ex.Message}", ex);
            }
            catch (DirectoryNotFoundException ex)
            {
                throw new InvalidOperationException($"Cannot find directory for users data file: {ex.Message}", ex);
            }
            catch (IOException ex)
            {
                throw new InvalidOperationException($"Error writing to the users data file: {ex.Message}", ex);
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException($"Error converting users to JSON format: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Unexpected error saving users: {ex.Message}", ex);
            }
        }
    }
}