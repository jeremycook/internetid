using System;
using System.ComponentModel.DataAnnotations;

namespace InternetId.Users.Data
{
    public class User
    {
        private string? _username;
        private string? _email;

        public override string ToString()
        {
            return Username;
        }

        public Guid Id { get; set; }

        [Required]
        [RegularExpression("^[a-zA-Z][a-zA-Z0-9]*$", ErrorMessage = "The {0} must start with a letter, and may only contain letters and numbers.")]
        public string Username { get => _username!; set => _username = value?.Trim(); }

        [Required]
        public string LowercaseUsername { get => Username?.ToLowerInvariant()!; private set { } }

        [Required]
        [Display(Name = "Display name")]
        public string DisplayName { get; set; } = null!;

        [DataType(DataType.EmailAddress)]
        public string? Email { get => _email; set => _email = value?.Trim(); }

        public string? LowercaseEmail { get => Email?.ToLowerInvariant(); private set { } }


        [Display(Name = "Email verified")]
        public bool EmailVerified { get; set; }

        public DateTimeOffset Created { get; set; }
    }
}
