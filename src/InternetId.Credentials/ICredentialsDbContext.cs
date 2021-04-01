using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace InternetId.Credentials
{
    public interface ICredentialsDbContext
    {
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
        DbSet<Credential> Credentials { get; }
    }
}