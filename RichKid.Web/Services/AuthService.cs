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

        public AuthService(HttpClient httpClient, IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
        {
            _httpClient = httpClient;
            _httpContextAccessor = httpContextAccessor;
            
            // Load endpoint URLs from config so we can change them without rebuilding
            _baseUrl = configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5270/api";
            _loginEndpoint = configuration["ApiSettings:Endpoints:Auth:Login"] ?? "/auth/login";
        }

        public async Task<AuthResult> LoginAsync(string username, string password)
        {
            try
            {
                // Don't bother making a request if credentials are empty
                if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                {
                    return new AuthResult { Success = false, ErrorMessage = "Username and password are required" };
                }

                // create the login payload using shared DTO for consistency
                var loginRequest = new LoginRequest { UserName = username, Password = password };
                var json = JsonSerializer.Serialize(loginRequest);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                Console.WriteLine($"=== AuthService.LoginAsync ===");
                Console.WriteLine($"Sending login request to: {_baseUrl}{_loginEndpoint}"); // Using config endpoint
                Console.WriteLine($"Username: {username}");

                var response = await _httpClient.PostAsync($"{_baseUrl}{_loginEndpoint}", content);

                Console.WriteLine($"Response Status: {response.StatusCode}");

                // Make sure we actually got a successful response before trying to read it
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    
                    // Sometimes APIs return 200 but with empty content - that's not right
                    if (string.IsNullOrWhiteSpace(responseContent))
                    {
                        Console.WriteLine("Warning: Got success status but empty response");
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
                            Console.WriteLine("Login successful");
                            return new AuthResult { Success = true, Token = tokenResponse.Token };
                        }
                        else
                        {
                            Console.WriteLine("Got success but no valid token in response");
                            return new AuthResult { Success = false, ErrorMessage = "Invalid token received from server" };
                        }
                    }
                    catch (JsonException ex)
                    {
                        Console.WriteLine($"Couldn't parse response JSON: {ex.Message}");
                        return new AuthResult { Success = false, ErrorMessage = "Invalid response format from server" };
                    }
                }

                // Handle unauthorized responses with specific error messages from API
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Login failed - Unauthorized: {errorContent}");
                    
                    // API might return error in quotes, so clean it up
                    var cleanError = string.IsNullOrWhiteSpace(errorContent) ? "Invalid credentials" : errorContent.Trim('"');
                    
                    return new AuthResult { Success = false, ErrorMessage = cleanError };
                }

                // Handle other error responses
                var generalError = await response.Content.ReadAsStringAsync();
                var errorMessage = string.IsNullOrWhiteSpace(generalError) ? "Login failed" : generalError.Trim('"');
                Console.WriteLine($"Login failed - {response.StatusCode}: {errorMessage}");
                return new AuthResult { Success = false, ErrorMessage = errorMessage };
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                Console.WriteLine("Login request timed out");
                return new AuthResult { Success = false, ErrorMessage = "Login request timed out. Please try again." };
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Network error during login: {ex.Message}");
                return new AuthResult { Success = false, ErrorMessage = "Connection error: Unable to reach authentication server" };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error during login: {ex.GetType().Name} - {ex.Message}");
                return new AuthResult { Success = false, ErrorMessage = "An unexpected error occurred during login" };
            }
        }

        public void SetAuthToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                Console.WriteLine("Warning: Trying to set empty token");
                return;
            }

            try
            {
                // add bearer token to HTTP client so all future requests are authenticated
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                
                // also store in session so it survives across different requests
                _httpContextAccessor.HttpContext?.Session.SetString("AuthToken", token);
                
                Console.WriteLine("Auth token stored successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error setting auth token: {ex.Message}");
            }
        }

        public void ClearAuthToken()
        {
            try
            {
                // remove from HTTP client headers
                _httpClient.DefaultRequestHeaders.Authorization = null;
                
                // clear from session storage too
                _httpContextAccessor.HttpContext?.Session.Remove("AuthToken");
                
                Console.WriteLine("Auth token cleared");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error clearing auth token: {ex.Message}");
            }
        }

        public string? GetCurrentToken()
        {
            try
            {
                // retrieve token from session storage
                return _httpContextAccessor.HttpContext?.Session.GetString("AuthToken");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting current token: {ex.Message}");
                return null;
            }
        }

        public bool IsAuthenticated()
        {
            // check if we have a token stored
            var token = GetCurrentToken();
            return !string.IsNullOrWhiteSpace(token);
        }
    }
}