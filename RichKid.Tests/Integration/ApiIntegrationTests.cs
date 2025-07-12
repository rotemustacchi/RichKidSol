using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Net;
using Xunit;
using RichKid.Shared.DTOs;
using RichKid.Shared.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using RichKid.Shared.Services;
using RichKid.API.Services;

namespace RichKid.Tests.Integration
{
    /// <summary>
    /// Integration tests for the RichKid API
    /// These tests verify that the entire API works correctly end-to-end,
    /// including authentication, authorization, and user management operations
    /// 
    /// IMPORTANT: These tests use a separate test data file to avoid interfering
    /// with your development Users.json file
    /// </summary>
    public class ApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;
        private readonly string _testDataFile;

        public ApiIntegrationTests(WebApplicationFactory<Program> factory)
        {
            // Create a unique test data file for this test run to avoid conflicts
            _testDataFile = Path.Combine(Path.GetTempPath(), $"TestUsers_{Guid.NewGuid():N}.json");
            
            // Set up a test version of our API with isolated data storage
            _factory = factory.WithWebHostBuilder(builder =>
            {
                // Configure the test environment to use separate data file
                builder.UseEnvironment("Testing");
                
                // Override the DataService to use our test file instead of the real Users.json
                builder.ConfigureServices(services =>
                {
                    // Remove the existing DataService registration
                    var dataServiceDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IDataService));
                    if (dataServiceDescriptor != null)
                    {
                        services.Remove(dataServiceDescriptor);
                    }
                    
                    // Register our test DataService that uses the test file
                    services.AddScoped<IDataService>(provider =>
                    {
                        var logger = provider.GetRequiredService<ILogger<TestDataService>>();
                        return new TestDataService(logger, _testDataFile);
                    });
                });
            });

            // Create HTTP client for making requests to our test API
            _client = _factory.CreateClient();
            
            // Initialize test data before running tests
            InitializeTestData();
        }

        /// <summary>
        /// Creates test data in our isolated test file
        /// This mimics the structure of the real Users.json but doesn't affect it
        /// </summary>
        private void InitializeTestData()
        {
            // Create test users that match the ones in your real Users.json
            // but store them in the separate test file
            var testUsers = new List<User>
            {
                new User 
                {
                    UserID = 1,
                    Active = true,
                    UserName = "Rotem",
                    Password = "1234",
                    UserGroupID = UserGroups.ADMIN, // Admin user for testing admin operations
                    Data = new UserData 
                    {
                        CreationDate = "2025-07-10",
                        FirstName = "Rotem",
                        LastName = "Mustacchi",
                        Phone = "0545326253",
                        Email = "rotem.mustacchi@gmail.com"
                    }
                },
                new User 
                {
                    UserID = 2,
                    Active = true,
                    UserName = "Alon",
                    Password = "1111",
                    UserGroupID = UserGroups.REGULAR_USER, // Regular user for testing limited permissions
                    Data = new UserData 
                    {
                        CreationDate = "",
                        FirstName = "Alon",
                        LastName = "Cohen",
                        Phone = "0525716539",
                        Email = "Alon@gmail.com"
                    }
                },
                new User 
                {
                    UserID = 3,
                    Active = false, // Inactive user for testing login restrictions
                    UserName = "CharliBrown",
                    Password = "33333333",
                    UserGroupID = UserGroups.VIEW_ONLY,
                    Data = new UserData 
                    {
                        CreationDate = "",
                        FirstName = "Charli",
                        LastName = "Brown",
                        Phone = "0503121243",
                        Email = "CharliBrown@gmail.com"
                    }
                },
                new User 
                {
                    UserID = 4,
                    Active = true,
                    UserName = "DanielaDanon",
                    Password = "ab!44",
                    UserGroupID = UserGroups.EDITOR, // Editor user for testing editor permissions
                    Data = new UserData 
                    {
                        CreationDate = "",
                        FirstName = "Daniela",
                        LastName = "Danon",
                        Phone = "",
                        Email = "Daniela@gmail.com"
                    }
                }
            };

            // Save test data to our isolated test file
            var wrapper = new { Users = testUsers };
            var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(wrapper, jsonOptions);
            File.WriteAllText(_testDataFile, json);
        }

        #region Authentication Integration Tests

        [Fact]
        public async Task Login_WithValidCredentials_ShouldReturnJwtToken()
        {
            // Arrange - Create a login request with test user credentials
            var loginRequest = new LoginRequest
            {
                UserName = "Rotem", // Using our test admin user
                Password = "1234"
            };

            var json = JsonSerializer.Serialize(loginRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act - Send login request to API
            var response = await _client.PostAsync("/api/auth/login", content);

            // Assert - Should receive a successful response with JWT token
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            
            var responseContent = await response.Content.ReadAsStringAsync();
            var loginResponse = JsonSerializer.Deserialize<LoginResponse>(responseContent, 
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            
            Assert.NotNull(loginResponse);
            Assert.NotEmpty(loginResponse.Token);
            
            // Verify the token is a valid JWT with correct claims
            var tokenHandler = new JwtSecurityTokenHandler();
            Assert.True(tokenHandler.CanReadToken(loginResponse.Token));
            
            var token = tokenHandler.ReadJwtToken(loginResponse.Token);
            Assert.Contains(token.Claims, c => c.Type == "sub" && c.Value == "Rotem");
            Assert.Contains(token.Claims, c => c.Type == "UserID" && c.Value == "1");
        }

        [Fact]
        public async Task Login_WithInvalidCredentials_ShouldReturnUnauthorized()
        {
            // Arrange - Use credentials that don't exist in our test data
            var loginRequest = new LoginRequest
            {
                UserName = "NonExistentUser",
                Password = "WrongPassword"
            };

            var json = JsonSerializer.Serialize(loginRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/auth/login", content);

            // Assert - Should be rejected with appropriate error message
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            
            var errorMessage = await response.Content.ReadAsStringAsync();
            Assert.Contains("Username not found", errorMessage);
        }

        [Fact]
        public async Task Login_WithInactiveUser_ShouldReturnUnauthorized()
        {
            // Arrange - Use inactive user credentials from our test data
            var loginRequest = new LoginRequest
            {
                UserName = "CharliBrown", // This user is inactive in our test data
                Password = "33333333"
            };

            var json = JsonSerializer.Serialize(loginRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/auth/login", content);

            // Assert - Should be rejected due to inactive status
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            
            var errorMessage = await response.Content.ReadAsStringAsync();
            Assert.Contains("inactive", errorMessage.ToLower());
        }

        #endregion

        #region User Management Integration Tests

        [Fact]
        public async Task GetUsers_WithoutAuthentication_ShouldReturnUnauthorized()
        {
            // Act - Try to access users endpoint without any authentication token
            var response = await _client.GetAsync("/api/users");

            // Assert - Should be rejected because no JWT token was provided
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetUsers_WithValidToken_ShouldReturnUserList()
        {
            // Arrange - First get a valid JWT token for authentication
            var token = await GetValidJwtTokenAsync();
            _client.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", token);

            // Act - Request users list with proper authentication
            var response = await _client.GetAsync("/api/users");

            // Assert - Should return successful response with user data from test file
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            
            var responseContent = await response.Content.ReadAsStringAsync();
            var users = JsonSerializer.Deserialize<List<User>>(responseContent, 
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            
            Assert.NotNull(users);
            Assert.True(users.Count >= 4); // Should have at least our test users
            
            // Verify the structure of returned users matches our test data
            var rotemUser = users.FirstOrDefault(u => u.UserName == "Rotem");
            Assert.NotNull(rotemUser);
            Assert.Equal(1, rotemUser.UserID);
            Assert.True(rotemUser.Active);
        }

        [Fact]
        public async Task CreateUser_WithAdminToken_ShouldCreateNewUser()
        {
            // Arrange - Get admin token and create new user data
            var adminToken = await GetValidJwtTokenAsync("Rotem", "1234"); // Admin user from test data
            _client.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", adminToken);

            var newUser = new User
            {
                UserName = "TestUser_" + Guid.NewGuid().ToString("N")[..8], // Unique username to avoid conflicts
                Password = "testpass123",
                Active = true,
                UserGroupID = UserGroups.REGULAR_USER,
                Data = new UserData
                {
                    FirstName = "Test",
                    LastName = "User",
                    Email = "test@example.com",
                    Phone = "123-456-7890"
                }
            };

            var json = JsonSerializer.Serialize(newUser);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act - Create the user using admin permissions
            var response = await _client.PostAsync("/api/users", content);

            // Assert - Should be created successfully and return proper location header
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            
            // Verify the location header points to the new user
            Assert.NotNull(response.Headers.Location);
            
            // Extract user ID from location header to verify creation
            var locationParts = response.Headers.Location.ToString().Split('/');
            var newUserId = locationParts.Last();
            Assert.True(int.TryParse(newUserId, out _));
        }

        [Fact]
        public async Task CreateUser_WithRegularUserToken_ShouldReturnForbidden()
        {
            // Arrange - Get regular user token (has limited permissions, cannot create users)
            var userToken = await GetValidJwtTokenAsync("Alon", "1111"); // Regular user from test data
            _client.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", userToken);

            var newUser = new User
            {
                UserName = "ShouldNotBeCreated",
                Password = "password",
                Active = true,
                Data = new UserData { FirstName = "Test", LastName = "User" }
            };

            var json = JsonSerializer.Serialize(newUser);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act - Try to create user without proper permissions
            var response = await _client.PostAsync("/api/users", content);

            // Assert - Should be forbidden because regular users can't create other users
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetUserById_WithValidId_ShouldReturnSpecificUser()
        {
            // Arrange - Get authentication token
            var token = await GetValidJwtTokenAsync();
            _client.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", token);

            // Act - Get specific user by ID (user 1 exists in our test data)
            var response = await _client.GetAsync("/api/users/1");

            // Assert - Should return the specific user from our test data
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            
            var responseContent = await response.Content.ReadAsStringAsync();
            var user = JsonSerializer.Deserialize<User>(responseContent, 
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            
            Assert.NotNull(user);
            Assert.Equal(1, user.UserID);
            Assert.Equal("Rotem", user.UserName); // Should match our test data
        }

        [Fact]
        public async Task GetUserById_WithInvalidId_ShouldReturnNotFound()
        {
            // Arrange - Get authentication token
            var token = await GetValidJwtTokenAsync();
            _client.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", token);

            // Act - Try to get user with ID that doesn't exist in test data
            var response = await _client.GetAsync("/api/users/99999");

            // Assert - Should return not found for non-existent user
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }


        [Fact]
        public async Task UpdateUser_WithValidData_ShouldUpdateSuccessfully()
        {
            // Arrange - Get admin token for update permissions
            var adminToken = await GetValidJwtTokenAsync("Rotem", "1234");
            _client.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", adminToken);

            // First, get an existing user to update (user 2 exists in our test data)
            var getUserResponse = await _client.GetAsync("/api/users/2");
            var existingUser = JsonSerializer.Deserialize<User>(
                await getUserResponse.Content.ReadAsStringAsync(),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            // Modify the user data to test the update functionality
            var originalFirstName = existingUser.Data.FirstName;
            existingUser.Data.FirstName = "UpdatedFirstName";
            existingUser.Data.LastName = "UpdatedLastName";

            var json = JsonSerializer.Serialize(existingUser);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act - Update the user via API
            var response = await _client.PutAsync($"/api/users/{existingUser.UserID}", content);

            // Assert - Should be updated successfully (returns 204 No Content)
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            // Verify the update persisted by retrieving the user again
            var verifyResponse = await _client.GetAsync($"/api/users/{existingUser.UserID}");
            var updatedUser = JsonSerializer.Deserialize<User>(
                await verifyResponse.Content.ReadAsStringAsync(),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            Assert.Equal("UpdatedFirstName", updatedUser.Data.FirstName);
            Assert.Equal("UpdatedLastName", updatedUser.Data.LastName);
            Assert.NotEqual(originalFirstName, updatedUser.Data.FirstName); // Confirm it actually changed
        }


        [Fact]
        public async Task DeleteUser_WithNonAdminToken_ShouldReturnForbidden()
        {
            // Arrange - Use regular user token (no delete permissions)
            var userToken = await GetValidJwtTokenAsync("Alon", "1111"); // Regular user from test data
            _client.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", userToken);

            // Act - Try to delete a user without proper permissions
            var response = await _client.DeleteAsync("/api/users/1");

            // Assert - Should be forbidden because regular users can't delete other users
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        #endregion

        #region Edge Cases and Error Handling

        [Fact]
        public async Task CreateUser_WithInvalidData_ShouldReturnBadRequest()
        {
            // Arrange - Get admin token for creation permissions
            var adminToken = await GetValidJwtTokenAsync("Rotem", "1234");
            _client.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", adminToken);

            var invalidUser = new User
            {
                UserName = "", // Invalid: empty username (violates validation rules)
                Password = "", // Invalid: empty password (violates validation rules)
                Data = new UserData
                {
                    FirstName = "", // Invalid: empty first name (violates validation rules)
                    Email = "invalid-email" // Invalid email format (violates validation rules)
                }
            };

            var json = JsonSerializer.Serialize(invalidUser);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act - Try to create user with invalid data
            var response = await _client.PostAsync("/api/users", content);

            // Assert - Should return validation errors
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Helper method to get a valid JWT token for testing authenticated endpoints
        /// Uses default admin credentials if not specified
        /// This allows tests to authenticate with the API using test user credentials
        /// </summary>
        private async Task<string> GetValidJwtTokenAsync(string username = "Rotem", string password = "1234")
        {
            var loginRequest = new LoginRequest
            {
                UserName = username,
                Password = password
            };

            var json = JsonSerializer.Serialize(loginRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _client.PostAsync("/api/auth/login", content);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new InvalidOperationException($"Failed to get JWT token: {response.StatusCode} - {errorContent}");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var loginResponse = JsonSerializer.Deserialize<LoginResponse>(responseContent, 
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return loginResponse?.Token ?? throw new InvalidOperationException("No token received");
        }

        #endregion

        /// <summary>
        /// Clean up test resources after tests complete
        /// This ensures our test data file doesn't clutter the temp directory
        /// </summary>
        public void Dispose()
        {
            // Clean up the test data file we created
            if (File.Exists(_testDataFile))
            {
                try
                {
                    File.Delete(_testDataFile);
                }
                catch
                {
                    // If cleanup fails, don't crash the test
                    // The temp file will be cleaned up by the OS eventually
                }
            }
            
            // Clean up HTTP client and factory resources
            _client?.Dispose();
            _factory?.Dispose();
        }
    }

    /// <summary>
    /// Test-specific version of DataService that uses a custom file path
    /// This prevents tests from interfering with the real Users.json file
    /// by storing test data in a separate temporary file
    /// </summary>
    public class TestDataService : IDataService
    {
        private readonly string _filePath;
        private readonly ILogger<TestDataService> _logger;

        public TestDataService(ILogger<TestDataService> logger, string filePath)
        {
            _logger = logger;
            _filePath = filePath;
            
            _logger.LogDebug("TestDataService initialized with test file: {FilePath}", _filePath);
        }

        /// <summary>
        /// Load users from the test data file
        /// Implements the same logic as the real DataService but uses test file
        /// </summary>
        public List<User> LoadUsers()
        {
            try
            {
                _logger.LogDebug("Loading test users from: {FilePath}", _filePath);
                
                // Return empty list if test file doesn't exist yet
                if (!File.Exists(_filePath))
                {
                    _logger.LogInformation("Test users file not found, returning empty list");
                    return new List<User>();
                }

                // Read and parse the JSON content from test file
                var json = File.ReadAllText(_filePath);
                
                if (string.IsNullOrWhiteSpace(json))
                {
                    _logger.LogWarning("Test users file is empty, returning empty list");
                    return new List<User>();
                }

                _logger.LogDebug("Test JSON content loaded, length: {JsonLength} characters", json.Length);

                // Parse the JSON and extract the Users array (same format as real file)
                using var doc = JsonDocument.Parse(json);
                
                if (!doc.RootElement.TryGetProperty("Users", out var usersElement))
                {
                    _logger.LogWarning("Test JSON file does not contain 'Users' property, returning empty list");
                    return new List<User>();
                }

                var users = usersElement.Deserialize<List<User>>();
                var userCount = users?.Count ?? 0;
                
                _logger.LogInformation("Successfully loaded {UserCount} test users", userCount);
                
                return users ?? new List<User>();
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse JSON from test users file: {FilePath}", _filePath);
                throw new InvalidOperationException($"The test users data file contains invalid JSON: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error loading test users from: {FilePath}", _filePath);
                throw new InvalidOperationException($"Unexpected error loading test users: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Save users to the test data file
        /// Implements the same logic as the real DataService but uses test file
        /// </summary>
        public void SaveUsers(List<User> users)
        {
            try
            {
                var userCount = users?.Count ?? 0;
                _logger.LogInformation("Saving {UserCount} test users to: {FilePath}", userCount, _filePath);
                
                if (users == null)
                {
                    _logger.LogWarning("Attempting to save null test users list, will save empty list instead");
                    users = new List<User>();
                }

                // Wrap users in an object structure for JSON (same format as real file)
                var wrapper = new { Users = users };
                
                // Serialize to JSON with readable formatting
                var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(wrapper, jsonOptions);
                
                _logger.LogDebug("Test JSON serialized successfully, length: {JsonLength} characters", json.Length);

                // Ensure the directory exists before writing test file
                var directory = Path.GetDirectoryName(_filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    _logger.LogDebug("Creating test directory: {Directory}", directory);
                    Directory.CreateDirectory(directory);
                }

                // Write JSON to test file
                File.WriteAllText(_filePath, json);
                
                _logger.LogInformation("Successfully saved {UserCount} test users", userCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error saving test users to: {FilePath}", _filePath);
                throw new InvalidOperationException($"Unexpected error saving test users: {ex.Message}", ex);
            }
        }
    }
}