using Microsoft.EntityFrameworkCore;

namespace InternetId.Credentials
{
    public class CredentialsDbContext : DbContext, ICredentialsDbContext
    {
        public CredentialsDbContext(DbContextOptions<CredentialsDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.Entity<Credential>().HasKey(o => new { o.Purpose, o.Key });
        }

        public DbSet<Credential> Credentials => Set<Credential>();
    }
}
