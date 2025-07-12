using RichKid.Shared.DTOs;

namespace RichKid.Shared.Services
{
    public interface IAuthService
    {
        Task<AuthResult> LoginAsync(string username, string password);
        void SetAuthToken(string token);
        void ClearAuthToken();
        string? GetCurrentToken();
        bool IsAuthenticated();
    }
}