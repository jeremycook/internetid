using InternetId.Users.Data;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Identity
{
    public static class UserManagerExtensions
    {
        public static async Task<User> FindByNameOrEmailAsync(this UserManager<User> userManager, string userNameOrEmail)
        {
            return
                await userManager.FindByNameAsync(userNameOrEmail) ??
                await userManager.FindByEmailAsync(userNameOrEmail);
        }
    }
}
