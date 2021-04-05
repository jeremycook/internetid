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

        public async Task<User?> FindByClientPrincipalAsync(ClaimsPrincipal claimsPrincipal)
        {
            var sub = claimsPrincipal.FindFirst("sub")?.Value;
            var client = claimsPrincipal.FindFirst("oi_prst")?.Value;

            if (sub is string subject && client is string clientId)
            {
                return await usersDb.UserClients
                    .Where(o => o.Subject == sub && o.ClientId == clientId)
                    .Select(o => o.User)
                    .SingleOrDefaultAsync();
            }
            else
            {
                return null;
            }
        }

        public async Task<User?> FindByLocalPrincipalAsync(ClaimsPrincipal claimsPrincipal)
        {
            var sub = claimsPrincipal.FindFirst("sub")?.Value;

            if (Guid.TryParse(sub, out Guid userId))
            {
                return await FindByIdAsync(userId);
            }
            else
            {
                return null;
            }
        }

        public async Task<User?> FindByIdAsync(Guid userId)
        {
            return await usersDb.Users.FindAsync(userId);
        }

        public async Task<User?> FindByUsernameAsync(string username)
        {
            username = username.Trim().ToLowerInvariant();

            return await usersDb.Users.SingleOrDefaultAsync(o => o.LowercaseUsername == username);
        }

        /// <summary>
        /// Returns the unambiguously matching <see cref="User"/> or <c>null</c>.
        /// <paramref name="identifier"/> can be a username or unique email.
        /// </summary>
        /// <param name="identifier">Username or unique email.</param>
        /// <returns></returns>
        public async Task<User?> FindByIdentifierAsync(string identifier)
        {
            identifier = identifier.Trim().ToLowerInvariant();

            User? user = await FindByUsernameAsync(identifier);

            if (user == null)
            {
                var candidates = await usersDb.Users
                    .Where(o => o.Email != null && o.Email.ToLower() == identifier)
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
