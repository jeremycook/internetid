using Microsoft.EntityFrameworkCore;

namespace InternetId.Common.Codes
{
    public class InternetIdCodesDbContext : DbContext
    {
        public InternetIdCodesDbContext(DbContextOptions<InternetIdCodesDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.Entity<Code>().HasKey(o => new { o.Purpose, o.Key });
        }

        public DbSet<Code> Codes => Set<Code>();
    }
}
