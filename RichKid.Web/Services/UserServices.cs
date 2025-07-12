using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using RichKid.Shared.Models;
using Microsoft.AspNetCore.Http;

namespace RichKid.Web.Services
{
    public interface IUserService
    {
        Task<List<User>> GetAllUsersAsync();
        Task AddUserAsync(User user);
        Task DeleteUserAsync(int id);
        Task UpdateUserAsync(User updatedUser);
        Task<User?> GetUserByIdAsync(int id);
        Task<List<User>> SearchByFullNameAsync(string firstName, string lastName);
    }

    public class UserService : IUserService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UserService(HttpClient httpClient, IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
        {
            _httpClient = httpClient;
            _baseUrl = configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5270/api";
            _httpContextAccessor = httpContextAccessor;
        }

        private void AddAuthHeader()
        {
            var token = _httpContextAccessor.HttpContext?.Session.GetString("AuthToken");
            
            Console.WriteLine($"=== UserService.AddAuthHeader ===");
            Console.WriteLine($"Token from session: {(string.IsNullOrEmpty(token) ? "NULL/EMPTY" : "EXISTS")}");
            Console.WriteLine($"Token length: {token?.Length ?? 0}");
            
            if (!string.IsNullOrEmpty(token))
            {
                // Clear any existing authorization header first
                _httpClient.DefaultRequestHeaders.Authorization = null;
                // Set the new one
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                Console.WriteLine($"Authorization header set successfully");
            }
            else
            {
                Console.WriteLine($"No token found - not setting auth header");
            }
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            try
            {
                Console.WriteLine($"=== UserService.GetAllUsersAsync START ===");
                Console.WriteLine($"Base URL: {_baseUrl}");
                
                AddAuthHeader();
                
                Console.WriteLine($"Making GET request to: {_baseUrl}/users");
                var response = await _httpClient.GetAsync($"{_baseUrl}/users");
                
                Console.WriteLine($"Response Status: {response.StatusCode}");
                Console.WriteLine($"Response Headers: {string.Join(", ", response.Headers.Select(h => $"{h.Key}={string.Join(",", h.Value)}"))}");
                
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    Console.WriteLine($"Unauthorized response - session may have expired");
                    throw new UnauthorizedAccessException("Session expired. Please login again.");
                }
                
                response.EnsureSuccessStatusCode();
                
                var json = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Response JSON length: {json?.Length ?? 0}");
                Console.WriteLine($"Response JSON (first 200 chars): {(string.IsNullOrEmpty(json) ? "EMPTY" : json.Substring(0, Math.Min(200, json.Length)))}");
                
                if (string.IsNullOrEmpty(json))
                {
                    Console.WriteLine($"Empty JSON response - returning empty list");
                    return new List<User>();
                }
                
                var users = JsonSerializer.Deserialize<List<User>>(json, new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true 
                });
                
                Console.WriteLine($"Deserialized {users?.Count ?? 0} users");
                Console.WriteLine($"=== UserService.GetAllUsersAsync END ===");
                
                return users ?? new List<User>();
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"HttpRequestException: {ex.Message}");
                throw new Exception($"Error fetching users: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"General Exception: {ex.GetType().Name} - {ex.Message}");
                throw;
            }
        }

        public async Task AddUserAsync(User user)
        {
            try
            {
                AddAuthHeader();
                
                var json = JsonSerializer.Serialize(user);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                Console.WriteLine($"=== UserService.AddUserAsync ===");
                Console.WriteLine($"Sending request to: {_baseUrl}/users");
                Console.WriteLine($"Request JSON: {json}");
                
                var response = await _httpClient.PostAsync($"{_baseUrl}/users", content);
                
                Console.WriteLine($"Response Status: {response.StatusCode}");
                
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    throw new UnauthorizedAccessException("Session expired. Please login again.");
                }
                
                if (!response.IsSuccessStatusCode)
                {
                    // Read the error response body to get the actual error message
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Error response content: {errorContent}");
                    
                    if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                    {
                        // Try to parse the error message from the API response
                        if (!string.IsNullOrEmpty(errorContent))
                        {
                            // Clean up the error message - remove quotes if it's a JSON string
                            var cleanError = errorContent.Trim('"');
                            
                            // The API now returns clean error messages
                            if (cleanError.Contains("Username already exists"))
                            {
                                throw new HttpRequestException("Username already exists in the system");
                            }
                            else
                            {
                                // Use the clean error message directly
                                throw new HttpRequestException(cleanError);
                            }
                        }
                        else
                        {
                            throw new HttpRequestException("The server could not process the request");
                        }
                    }
                    
                    // For other HTTP error codes
                    throw new HttpRequestException($"HTTP {(int)response.StatusCode} {response.ReasonPhrase}: {errorContent}");
                }
                
                Console.WriteLine("User added successfully");
            }
            catch (HttpRequestException)
            {
                // Re-throw HttpRequestException as-is (these contain our custom messages)
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"General Exception in AddUserAsync: {ex.GetType().Name} - {ex.Message}");
                throw new Exception($"Error adding user: {ex.Message}", ex);
            }
        }

        public async Task DeleteUserAsync(int id)
        {
            try
            {
                AddAuthHeader();
                
                var response = await _httpClient.DeleteAsync($"{_baseUrl}/users/{id}");
                
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    throw new UnauthorizedAccessException("Session expired. Please login again.");
                }
                
                response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Error deleting user: {ex.Message}", ex);
            }
        }

        public async Task UpdateUserAsync(User updatedUser)
        {
            try
            {
                AddAuthHeader();
                
                var json = JsonSerializer.Serialize(updatedUser);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                Console.WriteLine($"=== UserService.UpdateUserAsync ===");
                Console.WriteLine($"Sending request to: {_baseUrl}/users/{updatedUser.UserID}");
                
                var response = await _httpClient.PutAsync($"{_baseUrl}/users/{updatedUser.UserID}", content);
                
                Console.WriteLine($"Response Status: {response.StatusCode}");
                
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    throw new UnauthorizedAccessException("Session expired. Please login again.");
                }
                
                if (!response.IsSuccessStatusCode)
                {
                    // Read the error response body to get the actual error message
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Error response content: {errorContent}");
                    
                    if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                    {
                        if (!string.IsNullOrEmpty(errorContent))
                        {
                            // Clean up the error message - remove quotes if it's a JSON string
                            var cleanError = errorContent.Trim('"');
                            
                            if (cleanError.Contains("Username already exists"))
                            {
                                throw new HttpRequestException("Username already exists in the system");
                            }
                            else
                            {
                                throw new HttpRequestException(cleanError);
                            }
                        }
                        else
                        {
                            throw new HttpRequestException("The server could not process the request");
                        }
                    }
                    
                    throw new HttpRequestException($"HTTP {(int)response.StatusCode} {response.ReasonPhrase}: {errorContent}");
                }
                
                Console.WriteLine("User updated successfully");
            }
            catch (HttpRequestException)
            {
                // Re-throw HttpRequestException as-is
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"General Exception in UpdateUserAsync: {ex.GetType().Name} - {ex.Message}");
                throw new Exception($"Error updating user: {ex.Message}", ex);
            }
        }

        public async Task<User?> GetUserByIdAsync(int id)
        {
            try
            {
                AddAuthHeader();
                
                var response = await _httpClient.GetAsync($"{_baseUrl}/users/{id}");
                
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    return null;
                
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    throw new UnauthorizedAccessException("Session expired. Please login again.");
                }
                
                response.EnsureSuccessStatusCode();
                
                var json = await response.Content.ReadAsStringAsync();
                
                if (string.IsNullOrEmpty(json))
                    return null;
                
                var user = JsonSerializer.Deserialize<User>(json, new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true 
                });
                
                return user;
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Error fetching user by ID: {ex.Message}", ex);
            }
        }

        public async Task<List<User>> SearchByFullNameAsync(string firstName, string lastName)
        {
            try
            {
                AddAuthHeader();
                
                var response = await _httpClient.GetAsync($"{_baseUrl}/users/search?firstName={Uri.EscapeDataString(firstName)}&lastName={Uri.EscapeDataString(lastName)}");
                
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    throw new UnauthorizedAccessException("Session expired. Please login again.");
                }
                
                response.EnsureSuccessStatusCode();
                
                var json = await response.Content.ReadAsStringAsync();
                
                if (string.IsNullOrEmpty(json))
                {
                    return new List<User>();
                }
                
                var users = JsonSerializer.Deserialize<List<User>>(json, new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true 
                });
                
                return users ?? new List<User>();
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Error searching users: {ex.Message}", ex);
            }
        }
    }
}