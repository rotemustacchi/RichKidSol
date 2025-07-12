using RichKid.Shared.Models;

namespace RichKid.Shared.Services
{
    public interface IUserService
    {
        List<User> GetAllUsers();
        void AddUser(User user);
        void DeleteUser(int id);
        void UpdateUser(User updatedUser);
        User? GetUserById(int id);
        List<User> SearchByFullName(string first, string last);
    }
}