using Microsoft.EntityFrameworkCore;

namespace InternetId.Users.Data
{
    public class UsersDbContext : DbContext, IUsersDbContext
    {
        public UsersDbContext(DbContextOptions<UsersDbContext> options)
            : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(GetType().Assembly);

            if (modelBuilder.Entity<UserClient>() is var userClient)
            {
            }
        }

        public DbSet<User> Users { get; set; } = null!;
        public DbSet<UserClient> Clients { get; set; } = null!;
    }
}