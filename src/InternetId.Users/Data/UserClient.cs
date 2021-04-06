using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.ComponentModel.DataAnnotations;

namespace InternetId.Users.Data
{
    public class UserClient
    {
        public Guid UserId { get; set; }
        public virtual User? User { get; private set; }

        [Required]
        public string ClientId { get; set; } = null!;

        [Required]
        public string Subject { get; set; } = null!;

        public class EntityTypeConfiguration : IEntityTypeConfiguration<UserClient>
        {
            public void Configure(EntityTypeBuilder<UserClient> builder)
            {
                builder.HasKey(o => new { o.UserId, o.ClientId });
                builder.HasIndex(o => o.Subject);
            }
        }
    }
}
