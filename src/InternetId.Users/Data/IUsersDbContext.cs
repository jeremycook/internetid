using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System.Threading;
using System.Threading.Tasks;

namespace InternetId.Users.Data
{
    public interface IUsersDbContext
    {
        DatabaseFacade Database { get; }
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
        DbSet<User> Users { get; }
        DbSet<UserClient> Clients { get; }
    }
}
