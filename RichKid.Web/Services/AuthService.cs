using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using RichKid.Shared.DTOs;

namespace RichKid.Web.Services
{
    public interface IAuthService
    {
        Task<AuthResult> LoginAsync(string username, string password);
        void SetAuthToken(string token);
        void ClearAuthToken();
        string? GetCurrentToken();
        bool IsAuthenticated();
    }

    public class AuthService : IAuthService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuthService(HttpClient httpClient, IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
        {
            _httpClient = httpClient;
            _baseUrl = configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5270/api";
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<AuthResult> LoginAsync(string username, string password)
        {
            try
            {
                // create the login payload using shared DTO
                var loginRequest = new LoginRequest { UserName = username, Password = password };
                var json = JsonSerializer.Serialize(loginRequest);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                Console.WriteLine($"=== AuthService.LoginAsync ===");
                Console.WriteLine($"Sending login request to: {_baseUrl}/auth/login");
                Console.WriteLine($"Username: {username}");

                var response = await _httpClient.PostAsync($"{_baseUrl}/auth/login", content);

                Console.WriteLine($"Response Status: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var tokenResponse = JsonSerializer.Deserialize<LoginResponse>(responseContent, new JsonSerializerOptions 
                    { 
                        PropertyNameCaseInsensitive = true
                    });

                    if (tokenResponse?.Token != null)
                    {
                        SetAuthToken(tokenResponse.Token);
                        Console.WriteLine("Login successful");
                        return new AuthResult { Success = true, Token = tokenResponse.Token };
                    }
                }

                // Handle different error responses
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    // Read the error message from the API response
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Login failed - Unauthorized: {errorContent}");
                    
                    // Clean up the error message (remove quotes if it's a JSON string)
                    var cleanError = errorContent.Trim('"');
                    
                    return new AuthResult { Success = false, ErrorMessage = cleanError };
                }

                // For other error codes
                var generalError = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Login failed - {response.StatusCode}: {generalError}");
                return new AuthResult { Success = false, ErrorMessage = "Login failed" };
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"HttpRequestException during login: {ex.Message}");
                return new AuthResult { Success = false, ErrorMessage = $"Connection error: {ex.Message}" };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"General exception during login: {ex.Message}");
                return new AuthResult { Success = false, ErrorMessage = $"Login failed: {ex.Message}" };
            }
        }

        public void SetAuthToken(string token)
        {
            // add bearer token to all future HTTP requests
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            // also store in session for persistence across requests
            _httpContextAccessor.HttpContext?.Session.SetString("AuthToken", token);
        }

        public void ClearAuthToken()
        {
            // remove from HTTP client headers
            _httpClient.DefaultRequestHeaders.Authorization = null;
            // and clear from session
            _httpContextAccessor.HttpContext?.Session.Remove("AuthToken");
        }

        public string? GetCurrentToken()
        {
            // retrieve token from session storage
            return _httpContextAccessor.HttpContext?.Session.GetString("AuthToken");
        }

        public bool IsAuthenticated()
        {
            var token = GetCurrentToken();
            return !string.IsNullOrEmpty(token);
        }
    }
}