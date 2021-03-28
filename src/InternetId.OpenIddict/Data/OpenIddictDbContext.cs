using Microsoft.EntityFrameworkCore;

namespace InternetId.OpenIddict.Data
{
    public class OpenIddictDbContext : DbContext
    {
        public OpenIddictDbContext(DbContextOptions<OpenIddictDbContext> options)
            : base(options) { }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);

            optionsBuilder.UseOpenIddict();
        }
    }
}
