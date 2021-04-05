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
    }
}
