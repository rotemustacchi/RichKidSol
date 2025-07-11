using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

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
            _baseUrl = configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5270/api"; // fallback to localhost if config missing
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<AuthResult> LoginAsync(string username, string password)
        {
            try
            {
                // create the login payload
                var loginModel = new { UserName = username, Password = password };
                var json = JsonSerializer.Serialize(loginModel);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/auth/login", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(responseContent, new JsonSerializerOptions 
                    { 
                        PropertyNameCaseInsensitive = true // handle different casing from API
                    });

                    if (tokenResponse?.Token != null)
                    {
                        SetAuthToken(tokenResponse.Token); // store the token for future requests
                        return new AuthResult { Success = true, Token = tokenResponse.Token };
                    }
                }

                // if we get here, login failed
                return new AuthResult { Success = false, ErrorMessage = "Invalid credentials" };
            }
            catch (HttpRequestException ex)
            {
                // network issues, server down, etc.
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
            return !string.IsNullOrEmpty(token); // simple check - could be enhanced with expiry validation
        }
    }

    public class AuthResult
    {
        public bool Success { get; set; }
        public string? Token { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class TokenResponse
    {
        public string Token { get; set; } = string.Empty;
    }
}