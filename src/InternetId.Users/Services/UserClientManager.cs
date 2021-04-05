using InternetId.Users.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace InternetId.Server.Services
{
    public class UserClientManager
    {
        private const string sub = "sub";

        private readonly UsersDbContext usersDb;

        public UserClientManager(UsersDbContext usersDb)
        {
            this.usersDb = usersDb;
        }

        public async Task<ClaimsPrincipal> CreateClientPrincipalAsync(User user, string clientId)
        {
            string? subject = await GetOrCreateClientSubjectAsync(user, clientId);

            var claims = new List<Claim>
            {
                new Claim(sub, subject),
            };

            var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, GetType().FullName, sub, "role"));

            return principal;
        }

        public async Task<string> GetOrCreateClientSubjectAsync(User user, string clientId)
        {
            string? subject = await GetClientSubjectAsync(user, clientId);

            if (subject is null)
            {
                subject = Guid.NewGuid().ToString();
                usersDb.UserClients.Add(new UserClient
                {
                    UserId = user.Id,
                    ClientId = clientId,
                    Subject = subject
                });
                await usersDb.SaveChangesAsync();
            }

            return subject;
        }

        public async Task<string?> GetClientSubjectAsync(User user, string clientId)
        {
            return await usersDb.UserClients
                .Where(o => o.UserId == user.Id && o.ClientId == clientId)
                .Select(o => o.Subject)
                .SingleOrDefaultAsync();
        }

        public async Task<User?> FindByClientPrincipalAsync(ClaimsPrincipal claimsPrincipal)
        {
            var sub = claimsPrincipal.FindFirst(UserClientManager.sub)?.Value;
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
    }
}