using RichKid.Shared.Models;

namespace RichKid.Shared.Services
{
    public interface IDataService
    {
        List<User> LoadUsers();
        void SaveUsers(List<User> users);
    }
}