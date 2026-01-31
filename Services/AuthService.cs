using EasyLingo.Data.Entities;
using System.Threading.Tasks;

namespace EasyLingo.Services
{
    public class AuthService
    {
        private readonly DataService _dataService = new();

        public async Task<User?> LoginAsync(string username, string password)
        {
            var user = await _dataService.GetUserByUsernameAsync(username);
            if (user == null) return null;

            return PasswordHasher.VerifyPassword(password, user.PasswordHash)
                ? user
                : null;
        }
    }
}

