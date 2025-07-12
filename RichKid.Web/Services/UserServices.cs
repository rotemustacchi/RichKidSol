using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using RichKid.Shared.Models;
using RichKid.Shared.Services;
using Microsoft.AspNetCore.Http;

namespace RichKid.Web.Services
{
    // Implementing the shared interface from RichKid.Shared.Services
    public class UserService : IUserService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        // Get endpoints from config instead of hardcoding throughout the class
        private readonly string _usersEndpoint;
        private readonly string _userByIdEndpoint;
        private readonly string _usersSearchEndpoint;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<UserService> _logger; // Add logger for tracking user service operations

        public UserService(HttpClient httpClient, IConfiguration configuration, IHttpContextAccessor httpContextAccessor, ILogger<UserService> logger)
        {
            _httpClient = httpClient;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger; // Inject logger to monitor all user service operations
            
            // Load all API endpoint configurations from appsettings
            _baseUrl = configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5270/api";
            _usersEndpoint = configuration["ApiSettings:Endpoints:Users:Base"] ?? "/users";
            _userByIdEndpoint = configuration["ApiSettings:Endpoints:Users:GetById"] ?? "/users/{0}";
            _usersSearchEndpoint = configuration["ApiSettings:Endpoints:Users:Search"] ?? "/users/search";
            
            _logger.LogDebug("UserService initialized with base URL: {BaseUrl}, users endpoint: {UsersEndpoint}", 
                _baseUrl, _usersEndpoint);
        }

        private void AddAuthHeader()
        {
            var token = _httpContextAccessor.HttpContext?.Session.GetString("AuthToken");
            
            _logger.LogDebug("Adding authentication header - Token present: {HasToken}", 
                string.IsNullOrEmpty(token) ? "No" : "Yes");
            
            if (!string.IsNullOrEmpty(token))
            {
                // Clear existing header first to avoid conflicts
                _httpClient.DefaultRequestHeaders.Authorization = null;
                // Set the bearer token for API authentication
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                _logger.LogDebug("Bearer token set in authorization header");
            }
            else
            {
                _logger.LogWarning("No authentication token found - request will proceed without authentication");
            }
        }

        private void ValidateResponse(HttpResponseMessage response, string operation)
        {
            if (response.IsSuccessStatusCode)
            {
                _logger.LogDebug("HTTP request successful for operation: {Operation}, Status: {StatusCode}", 
                    operation, response.StatusCode);
                return; // All good, nothing to validate
            }

            _logger.LogWarning("HTTP error in operation: {Operation}, Status: {StatusCode}", operation, response.StatusCode);
            
            // Handle different error types with user-friendly messages
            switch (response.StatusCode)
            {
                case System.Net.HttpStatusCode.Unauthorized:
                    _logger.LogWarning("Session expired during operation: {Operation}", operation);
                    throw new UnauthorizedAccessException("Your session has expired. Please log in again to continue.");
                
                case System.Net.HttpStatusCode.Forbidden:
                    _logger.LogWarning("Permission denied during operation: {Operation}", operation);
                    throw new UnauthorizedAccessException("You don't have permission to perform this action. Please contact your administrator if you think this is an error.");
                
                case System.Net.HttpStatusCode.NotFound:
                    _logger.LogWarning("Resource not found during operation: {Operation}", operation);
                    throw new ArgumentException("The requested information could not be found. It may have been deleted or moved.");
                
                case System.Net.HttpStatusCode.BadRequest:
                    // Try to get the specific error message from API
                    var errorContent = response.Content.ReadAsStringAsync().Result;
                    _logger.LogWarning("Bad request during operation: {Operation}, Error content: {ErrorContent}", 
                        operation, errorContent ?? "No error details");
                    
                    // Handle username already exists specifically
                    if (!string.IsNullOrWhiteSpace(errorContent) && 
                        (errorContent.Contains("Username already exists") || errorContent.Contains("already exists")))
                    {
                        var cleanError = errorContent.Trim('"');
                        _logger.LogDebug("Username conflict detected: {ErrorMessage}", cleanError);
                        throw new HttpRequestException(cleanError);
                    }
                    
                    var cleanBadRequestError = string.IsNullOrWhiteSpace(errorContent) ? 
                        "There was a problem with your request. Please check your information and try again." : 
                        errorContent.Trim('"');
                    throw new HttpRequestException(cleanBadRequestError);
                
                case System.Net.HttpStatusCode.InternalServerError:
                    _logger.LogError("Server error during operation: {Operation}", operation);
                    throw new HttpRequestException("Something went wrong on our end. Please try again in a few moments.");
                
                case System.Net.HttpStatusCode.ServiceUnavailable:
                    _logger.LogError("Service unavailable during operation: {Operation}", operation);
                    throw new HttpRequestException("The service is temporarily unavailable. Please try again later.");
                
                default:
                    // Generic handler for other errors
                    var generalError = response.Content.ReadAsStringAsync().Result;
                    var errorMessage = string.IsNullOrWhiteSpace(generalError) ? 
                        "An unexpected error occurred. Please try again or contact support if the problem continues." : 
                        generalError.Trim('"');
                    _logger.LogError("Unexpected HTTP error during operation: {Operation}, Status: {StatusCode}, Error: {ErrorMessage}", 
                        operation, response.StatusCode, errorMessage);
                    throw new HttpRequestException(errorMessage);
            }
        }

        // Implementing shared interface methods
        public List<User> GetAllUsers()
        {
            try
            {
                _logger.LogInformation("Starting GetAllUsers request");
                
                AddAuthHeader(); // Make sure we're authenticated
                
                // Build full URL using config endpoint
                var fullUrl = $"{_baseUrl}{_usersEndpoint}";
                _logger.LogDebug("Making GET request to: {Url}", fullUrl);
                
                var response = _httpClient.GetAsync(fullUrl).Result;
                
                _logger.LogDebug("GetAllUsers response received with status: {StatusCode}", response.StatusCode);
                
                // Check if response is valid before trying to read it
                ValidateResponse(response, "GetAllUsers");
                
                var json = response.Content.ReadAsStringAsync().Result;
                _logger.LogDebug("Response JSON received, length: {JsonLength} characters", json?.Length ?? 0);
                
                // Handle empty responses gracefully
                if (string.IsNullOrEmpty(json))
                {
                    _logger.LogWarning("Empty response received from GetAllUsers - returning empty list");
                    return new List<User>();
                }
                
                try
                {
                    var users = JsonSerializer.Deserialize<List<User>>(json, new JsonSerializerOptions 
                    { 
                        PropertyNameCaseInsensitive = true 
                    });
                    
                    var userCount = users?.Count ?? 0;
                    _logger.LogInformation("GetAllUsers completed successfully. Retrieved {UserCount} users", userCount);
                    
                    return users ?? new List<User>();
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Failed to parse GetAllUsers JSON response");
                    throw new Exception("Invalid response format from server", ex);
                }
            }
            catch (UnauthorizedAccessException)
            {
                _logger.LogWarning("Authorization error in GetAllUsers");
                throw; // Let auth errors bubble up - these are already user-friendly
            }
            catch (HttpRequestException)
            {
                _logger.LogWarning("HTTP request error in GetAllUsers");
                throw; // Let HTTP errors bubble up - these now have user-friendly messages
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in GetAllUsers");
                throw new Exception("We're having trouble loading the user list. Please refresh the page or try again later.", ex);
            }
        }

        public void AddUser(User user)
        {
            try
            {
                _logger.LogInformation("Starting AddUser request for username: {Username}", user.UserName);
                
                // Basic validation before making the request
                if (user == null)
                {
                    _logger.LogError("AddUser called with null user object");
                    throw new ArgumentNullException(nameof(user), "User object cannot be null");
                }

                AddAuthHeader(); // Make sure we have auth token
                
                var json = JsonSerializer.Serialize(user);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                // Use config endpoint instead of hardcoded URL
                var fullUrl = $"{_baseUrl}{_usersEndpoint}";
                _logger.LogDebug("Sending POST request to: {Url} for user: {Username}", fullUrl, user.UserName);
                
                var response = _httpClient.PostAsync(fullUrl, content).Result;
                
                _logger.LogDebug("AddUser response received with status: {StatusCode} for user: {Username}", 
                    response.StatusCode, user.UserName);
                
                // Check if the request was successful
                ValidateResponse(response, "AddUser");
                
                _logger.LogInformation("AddUser completed successfully for user: {Username}", user.UserName);
            }
            catch (ArgumentNullException)
            {
                _logger.LogError("AddUser failed - User information is required");
                throw new ArgumentNullException("User information is required. Please fill out the form completely.");
            }
            catch (UnauthorizedAccessException)
            {
                _logger.LogWarning("Authorization error in AddUser for user: {Username}", user?.UserName ?? "Unknown");
                throw; // Let auth errors bubble up - these are already user-friendly
            }
            catch (HttpRequestException)
            {
                _logger.LogWarning("HTTP request error in AddUser for user: {Username}", user?.UserName ?? "Unknown");
                throw; // Let HTTP errors bubble up - these now have user-friendly messages
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in AddUser for user: {Username}", user?.UserName ?? "Unknown");
                throw new Exception("We couldn't create the new user. Please check your information and try again.", ex);
            }
        }

        public void DeleteUser(int id)
        {
            try
            {
                _logger.LogInformation("Starting DeleteUser request for user ID: {UserId}", id);
                
                // Make sure we have a valid ID
                if (id <= 0)
                {
                    _logger.LogError("DeleteUser called with invalid ID: {UserId}", id);
                    throw new ArgumentException("User ID must be a positive number", nameof(id));
                }

                AddAuthHeader(); // Set up authentication
                
                // Build URL with user ID using config endpoint pattern
                var deleteEndpoint = string.Format(_userByIdEndpoint, id);
                var fullUrl = $"{_baseUrl}{deleteEndpoint}";
                
                _logger.LogDebug("Sending DELETE request to: {Url} for user ID: {UserId}", fullUrl, id);
                
                var response = _httpClient.DeleteAsync(fullUrl).Result;
                
                _logger.LogDebug("DeleteUser response received with status: {StatusCode} for user ID: {UserId}", 
                    response.StatusCode, id);
                
                // Validate the response
                ValidateResponse(response, "DeleteUser");
                
                _logger.LogInformation("DeleteUser completed successfully for user ID: {UserId}", id);
            }
            catch (ArgumentException)
            {
                _logger.LogError("DeleteUser failed - Invalid user ID: {UserId}", id);
                throw new ArgumentException("Please select a valid user to delete.");
            }
            catch (UnauthorizedAccessException)
            {
                _logger.LogWarning("Authorization error in DeleteUser for user ID: {UserId}", id);
                throw; // Let auth errors bubble up - these are already user-friendly
            }
            catch (HttpRequestException)
            {
                _logger.LogWarning("HTTP request error in DeleteUser for user ID: {UserId}", id);
                throw; // Let HTTP errors bubble up - these now have user-friendly messages
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in DeleteUser for user ID: {UserId}", id);
                throw new Exception("We couldn't delete the user. Please try again or contact support if the problem continues.", ex);
            }
        }

        public void UpdateUser(User updatedUser)
        {
            try
            {
                _logger.LogInformation("Starting UpdateUser request for user: {Username} (ID: {UserId})", 
                    updatedUser.UserName, updatedUser.UserID);
                
                // Validate input
                if (updatedUser == null)
                {
                    _logger.LogError("UpdateUser called with null user object");
                    throw new ArgumentNullException(nameof(updatedUser), "User object cannot be null");
                }

                if (updatedUser.UserID <= 0)
                {
                    _logger.LogError("UpdateUser called with invalid user ID: {UserId}", updatedUser.UserID);
                    throw new ArgumentException("User ID must be a positive number", nameof(updatedUser));
                }

                AddAuthHeader(); // Set up authentication
                
                var json = JsonSerializer.Serialize(updatedUser);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                // Build URL with user ID using config endpoint
                var updateEndpoint = string.Format(_userByIdEndpoint, updatedUser.UserID);
                var fullUrl = $"{_baseUrl}{updateEndpoint}";
                _logger.LogDebug("Sending PUT request to: {Url} for user: {Username}", fullUrl, updatedUser.UserName);
                
                var response = _httpClient.PutAsync(fullUrl, content).Result;
                
                _logger.LogDebug("UpdateUser response received with status: {StatusCode} for user: {Username}", 
                    response.StatusCode, updatedUser.UserName);
                
                // Check if update was successful
                ValidateResponse(response, "UpdateUser");
                
                _logger.LogInformation("UpdateUser completed successfully for user: {Username} (ID: {UserId})", 
                    updatedUser.UserName, updatedUser.UserID);
            }
            catch (ArgumentNullException)
            {
                _logger.LogError("UpdateUser failed - User information is required");
                throw new ArgumentNullException("User information is required. Please fill out the form completely.");
            }
            catch (ArgumentException)
            {
                _logger.LogError("UpdateUser failed - Invalid user data provided");
                throw new ArgumentException("Please select a valid user to update.");
            }
            catch (UnauthorizedAccessException)
            {
                _logger.LogWarning("Authorization error in UpdateUser for user: {Username}", updatedUser?.UserName ?? "Unknown");
                throw; // Let auth errors bubble up - these are already user-friendly
            }
            catch (HttpRequestException)
            {
                _logger.LogWarning("HTTP request error in UpdateUser for user: {Username}", updatedUser?.UserName ?? "Unknown");
                throw; // Let HTTP errors bubble up - these now have user-friendly messages
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in UpdateUser for user: {Username}", updatedUser?.UserName ?? "Unknown");
                throw new Exception("We couldn't save the changes. Please check your information and try again.", ex);
            }
        }

        public User? GetUserById(int id)
        {
            try
            {
                _logger.LogDebug("Starting GetUserById request for user ID: {UserId}", id);
                
                // Validate ID first
                if (id <= 0)
                {
                    _logger.LogError("GetUserById called with invalid ID: {UserId}", id);
                    throw new ArgumentException("User ID must be a positive number", nameof(id));
                }

                AddAuthHeader(); // Set up authentication
                
                // Build URL with user ID using config endpoint
                var getUserEndpoint = string.Format(_userByIdEndpoint, id);
                var fullUrl = $"{_baseUrl}{getUserEndpoint}";
                
                _logger.LogDebug("Sending GET request to: {Url} for user ID: {UserId}", fullUrl, id);
                
                var response = _httpClient.GetAsync(fullUrl).Result;
                
                _logger.LogDebug("GetUserById response received with status: {StatusCode} for user ID: {UserId}", 
                    response.StatusCode, id);
                
                // 404 is valid - user just doesn't exist
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogDebug("User not found with ID: {UserId}", id);
                    return null;
                }
                
                // Check other response statuses
                ValidateResponse(response, "GetUserById");
                
                var json = response.Content.ReadAsStringAsync().Result;
                
                if (string.IsNullOrEmpty(json))
                {
                    _logger.LogWarning("Empty response received from GetUserById for user ID: {UserId}", id);
                    return null;
                }
                
                try
                {
                    var user = JsonSerializer.Deserialize<User>(json, new JsonSerializerOptions 
                    { 
                        PropertyNameCaseInsensitive = true 
                    });
                    
                    _logger.LogInformation("GetUserById completed successfully for user: {Username} (ID: {UserId})", 
                        user?.UserName ?? "Unknown", id);
                        
                    return user;
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Failed to parse GetUserById JSON response for user ID: {UserId}", id);
                    throw new Exception("Invalid response format from server", ex);
                }
            }
            catch (ArgumentException)
            {
                _logger.LogError("GetUserById failed - Invalid user ID: {UserId}", id);
                throw new ArgumentException("Please select a valid user to view.");
            }
            catch (UnauthorizedAccessException)
            {
                _logger.LogWarning("Authorization error in GetUserById for user ID: {UserId}", id);
                throw; // Let auth errors bubble up - these are already user-friendly
            }
            catch (HttpRequestException)
            {
                _logger.LogWarning("HTTP request error in GetUserById for user ID: {UserId}", id);
                throw; // Let HTTP errors bubble up - these now have user-friendly messages
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in GetUserById for user ID: {UserId}", id);
                throw new Exception("We couldn't load the user information. Please try again.", ex);
            }
        }

        public List<User> SearchByFullName(string first, string last)
        {
            try
            {
                _logger.LogInformation("Starting SearchByFullName request with firstName: '{FirstName}', lastName: '{LastName}'", 
                    first ?? "null", last ?? "null");
                
                // Need at least one name to search
                if (string.IsNullOrWhiteSpace(first) && string.IsNullOrWhiteSpace(last))
                {
                    _logger.LogError("SearchByFullName called with both names empty");
                    throw new ArgumentException("At least one name parameter must be provided");
                }

                AddAuthHeader(); // Set up authentication
                
                // Build query string from provided parameters
                var queryParams = new List<string>();
                if (!string.IsNullOrWhiteSpace(first))
                    queryParams.Add($"firstName={Uri.EscapeDataString(first)}");
                if (!string.IsNullOrWhiteSpace(last))
                    queryParams.Add($"lastName={Uri.EscapeDataString(last)}");
                
                var queryString = string.Join("&", queryParams);
                var fullUrl = $"{_baseUrl}{_usersSearchEndpoint}?{queryString}";
                
                _logger.LogDebug("Sending GET request to: {Url} for search", fullUrl);
                
                var response = _httpClient.GetAsync(fullUrl).Result;
                
                _logger.LogDebug("SearchByFullName response received with status: {StatusCode}", response.StatusCode);
                
                // Validate the response
                ValidateResponse(response, "SearchUsers");
                
                var json = response.Content.ReadAsStringAsync().Result;
                
                if (string.IsNullOrEmpty(json))
                {
                    _logger.LogDebug("Empty search results received - returning empty list");
                    return new List<User>();
                }
                
                try
                {
                    var users = JsonSerializer.Deserialize<List<User>>(json, new JsonSerializerOptions 
                    { 
                        PropertyNameCaseInsensitive = true 
                    });
                    
                    var resultCount = users?.Count ?? 0;
                    _logger.LogInformation("SearchByFullName completed successfully. Found {ResultCount} users matching criteria", resultCount);
                    
                    return users ?? new List<User>();
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Failed to parse SearchByFullName JSON response");
                    throw new Exception("Invalid response format from server", ex);
                }
            }
            catch (ArgumentException)
            {
                _logger.LogError("SearchByFullName failed - Invalid search parameters");
                throw new ArgumentException("Please enter at least a first name or last name to search.");
            }
            catch (UnauthorizedAccessException)
            {
                _logger.LogWarning("Authorization error in SearchByFullName");
                throw; // Let auth errors bubble up - these are already user-friendly
            }
            catch (HttpRequestException)
            {
                _logger.LogWarning("HTTP request error in SearchByFullName");
                throw; // Let HTTP errors bubble up - these now have user-friendly messages
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in SearchByFullName");
                throw new Exception("We couldn't search for users right now. Please try again later.", ex);
            }
        }

        // Additional async methods for better performance in web scenarios
        public async Task<List<User>> GetAllUsersAsync()
        {
            _logger.LogDebug("GetAllUsersAsync called - delegating to synchronous method");
            // Call the synchronous method directly to avoid Task.Run wrapping exceptions
            return GetAllUsers();
        }

        public async Task AddUserAsync(User user)
        {
            _logger.LogDebug("AddUserAsync called for user: {Username} - delegating to synchronous method", user.UserName);
            // Call the synchronous method directly to avoid Task.Run wrapping exceptions
            AddUser(user);
        }

        public async Task DeleteUserAsync(int id)
        {
            _logger.LogDebug("DeleteUserAsync called for user ID: {UserId} - delegating to synchronous method", id);
            // Call the synchronous method directly to avoid Task.Run wrapping exceptions
            DeleteUser(id);
        }

        public async Task UpdateUserAsync(User updatedUser)
        {
            _logger.LogDebug("UpdateUserAsync called for user: {Username} - delegating to synchronous method", updatedUser.UserName);
            // Call the synchronous method directly to avoid Task.Run wrapping exceptions
            UpdateUser(updatedUser);
        }

        public async Task<User?> GetUserByIdAsync(int id)
        {
            _logger.LogDebug("GetUserByIdAsync called for user ID: {UserId} - delegating to synchronous method", id);
            // Call the synchronous method directly to avoid Task.Run wrapping exceptions
            return GetUserById(id);
        }

        public async Task<List<User>> SearchByFullNameAsync(string firstName, string lastName)
        {
            _logger.LogDebug("SearchByFullNameAsync called - delegating to synchronous method");
            // Call the synchronous method directly to avoid Task.Run wrapping exceptions
            return SearchByFullName(firstName, lastName);
        }
    }
}