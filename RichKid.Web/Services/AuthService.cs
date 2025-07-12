using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using RichKid.Shared.DTOs;
using RichKid.Shared.Services;

namespace RichKid.Web.Services
{
    public class AuthService : IAuthService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly string _loginEndpoint; // Get login endpoint from config instead of hardcoding
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<AuthService> _logger; // Add logger for tracking authentication operations

        public AuthService(HttpClient httpClient, IConfiguration configuration, IHttpContextAccessor httpContextAccessor, ILogger<AuthService> logger)
        {
            _httpClient = httpClient;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger; // Inject logger to monitor all authentication activities
            
            // Load endpoint URLs from config so we can change them without rebuilding
            _baseUrl = configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5270/api";
            _loginEndpoint = configuration["ApiSettings:Endpoints:Auth:Login"] ?? "/auth/login";
            
            _logger.LogDebug("AuthService initialized with base URL: {BaseUrl}, login endpoint: {LoginEndpoint}", 
                _baseUrl, _loginEndpoint);
        }

        public async Task<AuthResult> LoginAsync(string username, string password)
        {
            try
            {
                _logger.LogInformation("Starting login process for username: {Username}", username);
                
                // Don't bother making a request if credentials are empty
                if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                {
                    _logger.LogWarning("Login failed - Empty credentials provided for username: {Username}", username ?? "null");
                    return new AuthResult { Success = false, ErrorMessage = "Username and password are required" };
                }

                // Create the login payload using shared DTO for consistency
                var loginRequest = new LoginRequest { UserName = username, Password = password };
                var json = JsonSerializer.Serialize(loginRequest);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var fullUrl = $"{_baseUrl}{_loginEndpoint}";
                _logger.LogDebug("Sending login request to: {LoginUrl} for username: {Username}", fullUrl, username);

                var response = await _httpClient.PostAsync(fullUrl, content);

                _logger.LogDebug("Received login response with status: {StatusCode} for username: {Username}", 
                    response.StatusCode, username);

                // Make sure we actually got a successful response before trying to read it
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    
                    // Sometimes APIs return 200 but with empty content - that's not right
                    if (string.IsNullOrWhiteSpace(responseContent))
                    {
                        _logger.LogWarning("Login received success status but empty response for username: {Username}", username);
                        return new AuthResult { Success = false, ErrorMessage = "Invalid response from server" };
                    }

                    try
                    {
                        var tokenResponse = JsonSerializer.Deserialize<LoginResponse>(responseContent, new JsonSerializerOptions 
                        { 
                            PropertyNameCaseInsensitive = true 
                        });

                        // Double-check we actually got a token back
                        if (tokenResponse?.Token != null && !string.IsNullOrWhiteSpace(tokenResponse.Token))
                        {
                            SetAuthToken(tokenResponse.Token); // Store it for future requests
                            _logger.LogInformation("Login successful for username: {Username}", username);
                            return new AuthResult { Success = true, Token = tokenResponse.Token };
                        }
                        else
                        {
                            _logger.LogWarning("Login received success but no valid token for username: {Username}", username);
                            return new AuthResult { Success = false, ErrorMessage = "Invalid token received from server" };
                        }
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogError(ex, "Failed to parse login response JSON for username: {Username}", username);
                        return new AuthResult { Success = false, ErrorMessage = "Invalid response format from server" };
                    }
                }

                // Handle unauthorized responses with specific error messages from API
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    
                    _logger.LogWarning("Login failed - Unauthorized for username: {Username}. Server response: {ErrorContent}", 
                        username, errorContent ?? "No error message");
                    
                    // API might return error in quotes, so clean it up
                    var cleanError = string.IsNullOrWhiteSpace(errorContent) ? "Invalid credentials" : errorContent.Trim('"');
                    
                    return new AuthResult { Success = false, ErrorMessage = cleanError };
                }

                // Handle other error responses
                var generalError = await response.Content.ReadAsStringAsync();
                var errorMessage = string.IsNullOrWhiteSpace(generalError) ? "Login failed" : generalError.Trim('"');
                
                _logger.LogWarning("Login failed for username: {Username} with status: {StatusCode}. Error: {ErrorMessage}", 
                    username, response.StatusCode, errorMessage);
                    
                return new AuthResult { Success = false, ErrorMessage = errorMessage };
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                _logger.LogError(ex, "Login request timed out for username: {Username}", username);
                return new AuthResult { Success = false, ErrorMessage = "Login request timed out. Please try again." };
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Network error during login for username: {Username}", username);
                return new AuthResult { Success = false, ErrorMessage = "Connection error: Unable to reach authentication server" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during login for username: {Username}", username);
                return new AuthResult { Success = false, ErrorMessage = "An unexpected error occurred during login" };
            }
        }

        public void SetAuthToken(string token)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(token))
                {
                    _logger.LogWarning("Attempting to set empty or null authentication token");
                    return;
                }

                // Add bearer token to HTTP client so all future requests are authenticated
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                
                // Also store in session so it survives across different requests
                _httpContextAccessor.HttpContext?.Session.SetString("AuthToken", token);
                
                _logger.LogDebug("Authentication token stored successfully in HTTP client and session");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while setting authentication token");
            }
        }

        public void ClearAuthToken()
        {
            try
            {
                _logger.LogDebug("Clearing authentication token from HTTP client and session");
                
                // Remove from HTTP client headers
                _httpClient.DefaultRequestHeaders.Authorization = null;
                
                // Clear from session storage too
                _httpContextAccessor.HttpContext?.Session.Remove("AuthToken");
                
                _logger.LogInformation("Authentication token cleared successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while clearing authentication token");
            }
        }

        public string? GetCurrentToken()
        {
            try
            {
                // Retrieve token from session storage
                var token = _httpContextAccessor.HttpContext?.Session.GetString("AuthToken");
                
                _logger.LogDebug("Retrieved authentication token from session: {HasToken}", 
                    string.IsNullOrEmpty(token) ? "None" : "Present");
                    
                return token;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving current authentication token");
                return null;
            }
        }

        public bool IsAuthenticated()
        {
            try
            {
                // Check if we have a token stored
                var token = GetCurrentToken();
                var isAuthenticated = !string.IsNullOrWhiteSpace(token);
                
                _logger.LogDebug("Authentication status checked: {IsAuthenticated}", isAuthenticated);
                
                return isAuthenticated;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while checking authentication status");
                return false; // Default to not authenticated on errors
            }
        }
    }
}