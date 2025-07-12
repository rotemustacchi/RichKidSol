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

        public UserService(HttpClient httpClient, IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
        {
            _httpClient = httpClient;
            _httpContextAccessor = httpContextAccessor;
            
            // Load all API endpoint configurations from appsettings
            _baseUrl = configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5270/api";
            _usersEndpoint = configuration["ApiSettings:Endpoints:Users:Base"] ?? "/users";
            _userByIdEndpoint = configuration["ApiSettings:Endpoints:Users:GetById"] ?? "/users/{0}";
            _usersSearchEndpoint = configuration["ApiSettings:Endpoints:Users:Search"] ?? "/users/search";
        }

        private void AddAuthHeader()
        {
            var token = _httpContextAccessor.HttpContext?.Session.GetString("AuthToken");
            
            Console.WriteLine($"=== UserService.AddAuthHeader ===");
            Console.WriteLine($"Token from session: {(string.IsNullOrEmpty(token) ? "NULL/EMPTY" : "EXISTS")}");
            Console.WriteLine($"Token length: {token?.Length ?? 0}");
            
            if (!string.IsNullOrEmpty(token))
            {
                // Clear existing header first to avoid conflicts
                _httpClient.DefaultRequestHeaders.Authorization = null;
                // Set the bearer token for API authentication
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                Console.WriteLine($"Authorization header set successfully");
            }
            else
            {
                Console.WriteLine($"No token found - request will go without auth");
            }
        }

        private void ValidateResponse(HttpResponseMessage response, string operation)
        {
            if (response.IsSuccessStatusCode)
                return; // All good, nothing to validate

            Console.WriteLine($"HTTP Error in {operation}: {response.StatusCode}");
            
            // Handle different error types with user-friendly messages
            switch (response.StatusCode)
            {
                case System.Net.HttpStatusCode.Unauthorized:
                    Console.WriteLine($"Session expired in {operation}");
                    throw new UnauthorizedAccessException("Your session has expired. Please log in again to continue.");
                
                case System.Net.HttpStatusCode.Forbidden:
                    Console.WriteLine($"Permission denied in {operation}");
                    throw new UnauthorizedAccessException("You don't have permission to perform this action. Please contact your administrator if you think this is an error.");
                
                case System.Net.HttpStatusCode.NotFound:
                    Console.WriteLine($"Resource not found in {operation}");
                    throw new ArgumentException("The requested information could not be found. It may have been deleted or moved.");
                
                case System.Net.HttpStatusCode.BadRequest:
                    // Try to get the specific error message from API
                    var errorContent = response.Content.ReadAsStringAsync().Result;
                    Console.WriteLine($"Bad request error content: {errorContent}");
                    
                    // Handle username already exists specifically
                    if (!string.IsNullOrWhiteSpace(errorContent) && 
                        (errorContent.Contains("Username already exists") || errorContent.Contains("already exists")))
                    {
                        var cleanError = errorContent.Trim('"');
                        Console.WriteLine($"Username conflict detected: {cleanError}");
                        throw new HttpRequestException(cleanError);
                    }
                    
                    var cleanBadRequestError = string.IsNullOrWhiteSpace(errorContent) ? 
                        "There was a problem with your request. Please check your information and try again." : 
                        errorContent.Trim('"');
                    Console.WriteLine($"Bad request in {operation}: {cleanBadRequestError}");
                    throw new HttpRequestException(cleanBadRequestError);
                
                case System.Net.HttpStatusCode.InternalServerError:
                    Console.WriteLine($"Server error in {operation}");
                    throw new HttpRequestException("Something went wrong on our end. Please try again in a few moments.");
                
                case System.Net.HttpStatusCode.ServiceUnavailable:
                    Console.WriteLine($"Service unavailable in {operation}");
                    throw new HttpRequestException("The service is temporarily unavailable. Please try again later.");
                
                default:
                    // Generic handler for other errors
                    var generalError = response.Content.ReadAsStringAsync().Result;
                    var errorMessage = string.IsNullOrWhiteSpace(generalError) ? 
                        "An unexpected error occurred. Please try again or contact support if the problem continues." : 
                        generalError.Trim('"');
                    Console.WriteLine($"HTTP error in {operation}: {errorMessage}");
                    throw new HttpRequestException(errorMessage);
            }
        }

        // Implementing shared interface methods
        public List<User> GetAllUsers()
        {
            try
            {
                Console.WriteLine($"=== UserService.GetAllUsers START ===");
                Console.WriteLine($"Base URL: {_baseUrl}");
                Console.WriteLine($"Users endpoint: {_usersEndpoint}");
                
                AddAuthHeader(); // Make sure we're authenticated
                
                // Build full URL using config endpoint
                var fullUrl = $"{_baseUrl}{_usersEndpoint}";
                Console.WriteLine($"Making GET request to: {fullUrl}");
                
                var response = _httpClient.GetAsync(fullUrl).Result;
                
                Console.WriteLine($"Response Status: {response.StatusCode}");
                Console.WriteLine($"Response Headers: {string.Join(", ", response.Headers.Select(h => $"{h.Key}={string.Join(",", h.Value)}"))}");
                
                // Check if response is valid before trying to read it
                ValidateResponse(response, "GetAllUsers");
                
                var json = response.Content.ReadAsStringAsync().Result;
                Console.WriteLine($"Response JSON length: {json?.Length ?? 0}");
                Console.WriteLine($"Response JSON (first 200 chars): {(string.IsNullOrEmpty(json) ? "EMPTY" : json.Substring(0, Math.Min(200, json.Length)))}");
                
                // Handle empty responses gracefully
                if (string.IsNullOrEmpty(json))
                {
                    Console.WriteLine($"Empty response - returning empty list");
                    return new List<User>();
                }
                
                try
                {
                    var users = JsonSerializer.Deserialize<List<User>>(json, new JsonSerializerOptions 
                    { 
                        PropertyNameCaseInsensitive = true 
                    });
                    
                    Console.WriteLine($"Successfully got {users?.Count ?? 0} users");
                    Console.WriteLine($"=== UserService.GetAllUsers END ===");
                    
                    return users ?? new List<User>();
                }
                catch (JsonException ex)
                {
                    Console.WriteLine($"JSON parsing failed: {ex.Message}");
                    throw new Exception("Invalid response format from server", ex);
                }
            }
            catch (UnauthorizedAccessException)
            {
                throw; // Let auth errors bubble up - these are already user-friendly
            }
            catch (HttpRequestException)
            {
                throw; // Let HTTP errors bubble up - these now have user-friendly messages
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error in GetAllUsers: {ex.GetType().Name} - {ex.Message}");
                throw new Exception("We're having trouble loading the user list. Please refresh the page or try again later.", ex);
            }
        }

        public void AddUser(User user)
        {
            try
            {
                // Basic validation before making the request
                if (user == null)
                {
                    throw new ArgumentNullException(nameof(user), "User object cannot be null");
                }

                AddAuthHeader(); // Make sure we have auth token
                
                var json = JsonSerializer.Serialize(user);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                Console.WriteLine($"=== UserService.AddUser ===");
                // Use config endpoint instead of hardcoded URL
                var fullUrl = $"{_baseUrl}{_usersEndpoint}";
                Console.WriteLine($"Sending POST request to: {fullUrl}");
                Console.WriteLine($"Request JSON: {json}");
                
                var response = _httpClient.PostAsync(fullUrl, content).Result;
                
                Console.WriteLine($"Response Status: {response.StatusCode}");
                
                // Check if the request was successful
                ValidateResponse(response, "AddUser");
                
                Console.WriteLine("User added successfully");
            }
            catch (ArgumentNullException)
            {
                throw new ArgumentNullException("User information is required. Please fill out the form completely.");
            }
            catch (UnauthorizedAccessException)
            {
                throw; // Let auth errors bubble up - these are already user-friendly
            }
            catch (HttpRequestException)
            {
                throw; // Let HTTP errors bubble up - these now have user-friendly messages
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error in AddUser: {ex.GetType().Name} - {ex.Message}");
                throw new Exception("We couldn't create the new user. Please check your information and try again.", ex);
            }
        }

        public void DeleteUser(int id)
        {
            try
            {
                // Make sure we have a valid ID
                if (id <= 0)
                {
                    throw new ArgumentException("User ID must be a positive number", nameof(id));
                }

                AddAuthHeader(); // Set up authentication
                
                // Build URL with user ID using config endpoint pattern
                var deleteEndpoint = string.Format(_userByIdEndpoint, id);
                var fullUrl = $"{_baseUrl}{deleteEndpoint}";
                
                Console.WriteLine($"=== UserService.DeleteUser ===");
                Console.WriteLine($"Sending DELETE request to: {fullUrl}");
                
                var response = _httpClient.DeleteAsync(fullUrl).Result;
                
                Console.WriteLine($"Response Status: {response.StatusCode}");
                
                // Validate the response
                ValidateResponse(response, "DeleteUser");
                
                Console.WriteLine("User deleted successfully");
            }
            catch (ArgumentException)
            {
                throw new ArgumentException("Please select a valid user to delete.");
            }
            catch (UnauthorizedAccessException)
            {
                throw; // Let auth errors bubble up - these are already user-friendly
            }
            catch (HttpRequestException)
            {
                throw; // Let HTTP errors bubble up - these now have user-friendly messages
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error in DeleteUser: {ex.GetType().Name} - {ex.Message}");
                throw new Exception("We couldn't delete the user. Please try again or contact support if the problem continues.", ex);
            }
        }

        public void UpdateUser(User updatedUser)
        {
            try
            {
                // Validate input
                if (updatedUser == null)
                {
                    throw new ArgumentNullException(nameof(updatedUser), "User object cannot be null");
                }

                if (updatedUser.UserID <= 0)
                {
                    throw new ArgumentException("User ID must be a positive number", nameof(updatedUser));
                }

                AddAuthHeader(); // Set up authentication
                
                var json = JsonSerializer.Serialize(updatedUser);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                Console.WriteLine($"=== UserService.UpdateUser ===");
                
                // Build URL with user ID using config endpoint
                var updateEndpoint = string.Format(_userByIdEndpoint, updatedUser.UserID);
                var fullUrl = $"{_baseUrl}{updateEndpoint}";
                Console.WriteLine($"Sending PUT request to: {fullUrl}");
                
                var response = _httpClient.PutAsync(fullUrl, content).Result;
                
                Console.WriteLine($"Response Status: {response.StatusCode}");
                
                // Check if update was successful
                ValidateResponse(response, "UpdateUser");
                
                Console.WriteLine("User updated successfully");
            }
            catch (ArgumentNullException)
            {
                throw new ArgumentNullException("User information is required. Please fill out the form completely.");
            }
            catch (ArgumentException)
            {
                throw new ArgumentException("Please select a valid user to update.");
            }
            catch (UnauthorizedAccessException)
            {
                throw; // Let auth errors bubble up - these are already user-friendly
            }
            catch (HttpRequestException)
            {
                throw; // Let HTTP errors bubble up - these now have user-friendly messages
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error in UpdateUser: {ex.GetType().Name} - {ex.Message}");
                throw new Exception("We couldn't save the changes. Please check your information and try again.", ex);
            }
        }

        public User? GetUserById(int id)
        {
            try
            {
                // Validate ID first
                if (id <= 0)
                {
                    throw new ArgumentException("User ID must be a positive number", nameof(id));
                }

                AddAuthHeader(); // Set up authentication
                
                // Build URL with user ID using config endpoint
                var getUserEndpoint = string.Format(_userByIdEndpoint, id);
                var fullUrl = $"{_baseUrl}{getUserEndpoint}";
                
                Console.WriteLine($"=== UserService.GetUserById ===");
                Console.WriteLine($"Sending GET request to: {fullUrl}");
                
                var response = _httpClient.GetAsync(fullUrl).Result;
                
                Console.WriteLine($"Response Status: {response.StatusCode}");
                
                // 404 is valid - user just doesn't exist
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    Console.WriteLine("User not found - returning null");
                    return null;
                }
                
                // Check other response statuses
                ValidateResponse(response, "GetUserById");
                
                var json = response.Content.ReadAsStringAsync().Result;
                
                if (string.IsNullOrEmpty(json))
                {
                    Console.WriteLine("Empty response - returning null");
                    return null;
                }
                
                try
                {
                    var user = JsonSerializer.Deserialize<User>(json, new JsonSerializerOptions 
                    { 
                        PropertyNameCaseInsensitive = true 
                    });
                    
                    Console.WriteLine($"Successfully got user: {user?.UserName ?? "null"}");
                    return user;
                }
                catch (JsonException ex)
                {
                    Console.WriteLine($"JSON parsing failed: {ex.Message}");
                    throw new Exception("Invalid response format from server", ex);
                }
            }
            catch (ArgumentException)
            {
                throw new ArgumentException("Please select a valid user to view.");
            }
            catch (UnauthorizedAccessException)
            {
                throw; // Let auth errors bubble up - these are already user-friendly
            }
            catch (HttpRequestException)
            {
                throw; // Let HTTP errors bubble up - these now have user-friendly messages
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error in GetUserById: {ex.GetType().Name} - {ex.Message}");
                throw new Exception("We couldn't load the user information. Please try again.", ex);
            }
        }

        public List<User> SearchByFullName(string first, string last)
        {
            try
            {
                // Need at least one name to search
                if (string.IsNullOrWhiteSpace(first) && string.IsNullOrWhiteSpace(last))
                {
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
                var fullUrl = $"{_baseUrl}{_usersSearchEndpoint}?{queryString}"; // Using config endpoint
                
                Console.WriteLine($"=== UserService.SearchByFullName ===");
                Console.WriteLine($"Sending GET request to: {fullUrl}");
                
                var response = _httpClient.GetAsync(fullUrl).Result;
                
                Console.WriteLine($"Response Status: {response.StatusCode}");
                
                // Validate the response
                ValidateResponse(response, "SearchUsers");
                
                var json = response.Content.ReadAsStringAsync().Result;
                
                if (string.IsNullOrEmpty(json))
                {
                    Console.WriteLine("Empty search results - returning empty list");
                    return new List<User>();
                }
                
                try
                {
                    var users = JsonSerializer.Deserialize<List<User>>(json, new JsonSerializerOptions 
                    { 
                        PropertyNameCaseInsensitive = true 
                    });
                    
                    Console.WriteLine($"Search found {users?.Count ?? 0} users");
                    return users ?? new List<User>();
                }
                catch (JsonException ex)
                {
                    Console.WriteLine($"JSON parsing failed in search: {ex.Message}");
                    throw new Exception("Invalid response format from server", ex);
                }
            }
            catch (ArgumentException)
            {
                throw new ArgumentException("Please enter at least a first name or last name to search.");
            }
            catch (UnauthorizedAccessException)
            {
                throw; // Let auth errors bubble up - these are already user-friendly
            }
            catch (HttpRequestException)
            {
                throw; // Let HTTP errors bubble up - these now have user-friendly messages
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error in SearchUsers: {ex.GetType().Name} - {ex.Message}");
                throw new Exception("We couldn't search for users right now. Please try again later.", ex);
            }
        }

        // Additional async methods for better performance in web scenarios
        public async Task<List<User>> GetAllUsersAsync()
        {
            // Call the synchronous method directly to avoid Task.Run wrapping exceptions
            return GetAllUsers();
        }

        public async Task AddUserAsync(User user)
        {
            // Call the synchronous method directly to avoid Task.Run wrapping exceptions
            AddUser(user);
        }

        public async Task DeleteUserAsync(int id)
        {
            // Call the synchronous method directly to avoid Task.Run wrapping exceptions
            DeleteUser(id);
        }

        public async Task UpdateUserAsync(User updatedUser)
        {
            // Call the synchronous method directly to avoid Task.Run wrapping exceptions
            UpdateUser(updatedUser);
        }

        public async Task<User?> GetUserByIdAsync(int id)
        {
            // Call the synchronous method directly to avoid Task.Run wrapping exceptions
            return GetUserById(id);
        }

        public async Task<List<User>> SearchByFullNameAsync(string firstName, string lastName)
        {
            // Call the synchronous method directly to avoid Task.Run wrapping exceptions
            return SearchByFullName(firstName, lastName);
        }
    }
}