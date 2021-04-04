using InternetId.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace InternetId.Users.Data
{
    public class UsersDbContext : DbContext
    {
        public UsersDbContext(DbContextOptions<UsersDbContext> options)
            : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            if (modelBuilder.Entity<User>() is var user)
            {
                user.Property(o => o.LowercaseUsername).HasComputedColumnSql("lower(username)", stored: true);
                user.HasIndex(o => o.LowercaseUsername).IsUnique();

                user.Property(o => o.LowercaseEmail).HasComputedColumnSql("lower(email)", stored: true);
                user.HasIndex(o => o.LowercaseEmail);

                user.Property(o => o.Created).HasDefaultValueSql("current_timestamp");
            }

            modelBuilder.ApplyUnderscoreNames();
        }

        public DbSet<User> Users => Set<User>();
    }
}