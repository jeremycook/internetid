using InternetId.Users.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace InternetId.Users.Services
{
    public class UserFinder
    {
        private readonly UsersDbContext usersDb;

        public UserFinder(UsersDbContext usersDb)
        {
            this.usersDb = usersDb;
        }

        public async Task<User?> FindUserAsync(ClaimsPrincipal claimsPrincipal)
        {
            var sub = claimsPrincipal.FindFirst("sub")?.Value;

            if (Guid.TryParse(sub, out Guid userId))
            {
                return await usersDb.Users.FindAsync(userId);
            }
            else
            {
                return null;
            }
        }

        public async Task<User?> FindByUsernameAsync(string username)
        {
            username = username.Trim().ToLowerInvariant();

            return await usersDb.Users.SingleOrDefaultAsync(o => o.LowercaseUsername == username);
        }

        public async Task<User?> FindAsync(string login)
        {
            login = login.Trim().ToLowerInvariant();

            var user = await FindByUsernameAsync(login);

            if (user == null)
            {
                var candidates = await usersDb.Users
                    .Where(o => o.Email != null && o.Email.ToLower() == login)
                    .Take(2)
                    .ToArrayAsync();

                if (candidates.Length == 1)
                {
                    user = candidates[0];
                }
            }

            return user;
        }
    }
}
