using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using RichKid.Web.Models;
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
            _baseUrl = configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5270/api"; // default to local dev server
            _httpContextAccessor = httpContextAccessor;
        }

        private void AddAuthHeader()
        {
            // grab the auth token from session and add to request headers
            var token = _httpContextAccessor.HttpContext?.Session.GetString("AuthToken");
            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            try
            {
                AddAuthHeader(); // make sure we're authenticated
                
                var response = await _httpClient.GetAsync($"{_baseUrl}/users");
                
                // check if user session expired
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    throw new UnauthorizedAccessException("Session expired. Please login again.");
                }
                
                response.EnsureSuccessStatusCode();
                
                var json = await response.Content.ReadAsStringAsync();
                var users = JsonSerializer.Deserialize<List<User>>(json, new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true // handle API response casing differences
                });
                
                return users ?? new List<User>(); // return empty list if null
            }
            catch (HttpRequestException ex)
            {
                // wrap in more descriptive exception
                throw new Exception($"Error fetching users: {ex.Message}", ex);
            }
        }

        public async Task AddUserAsync(User user)
        {
            try
            {
                AddAuthHeader(); // need auth for POST operations
                
                var json = JsonSerializer.Serialize(user);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PostAsync($"{_baseUrl}/users", content);
                
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    throw new UnauthorizedAccessException("Session expired. Please login again.");
                }
                
                response.EnsureSuccessStatusCode(); // will throw if not 2xx
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Error adding user: {ex.Message}", ex);
            }
        }

        public async Task DeleteUserAsync(int id)
        {
            try
            {
                AddAuthHeader(); // auth required for deletion
                
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
                AddAuthHeader(); // auth needed for updates
                
                var json = JsonSerializer.Serialize(updatedUser);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                // use the user's ID in the URL path
                var response = await _httpClient.PutAsync($"{_baseUrl}/users/{updatedUser.UserID}", content);
                
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    throw new UnauthorizedAccessException("Session expired. Please login again.");
                }
                
                response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Error updating user: {ex.Message}", ex);
            }
        }

        public async Task<User?> GetUserByIdAsync(int id)
        {
            try
            {
                AddAuthHeader();
                
                var response = await _httpClient.GetAsync($"{_baseUrl}/users/{id}");
                
                // return null if user not found
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    return null;
                
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    throw new UnauthorizedAccessException("Session expired. Please login again.");
                }
                
                response.EnsureSuccessStatusCode();
                
                var json = await response.Content.ReadAsStringAsync();
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
                
                // URL encode the search parameters to handle special characters
                var response = await _httpClient.GetAsync($"{_baseUrl}/users/search?firstName={Uri.EscapeDataString(firstName)}&lastName={Uri.EscapeDataString(lastName)}");
                
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    throw new UnauthorizedAccessException("Session expired. Please login again.");
                }
                
                response.EnsureSuccessStatusCode();
                
                var json = await response.Content.ReadAsStringAsync();
                var users = JsonSerializer.Deserialize<List<User>>(json, new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true 
                });
                
                return users ?? new List<User>(); // fallback to empty list
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Error searching users: {ex.Message}", ex);
            }
        }
    }
}