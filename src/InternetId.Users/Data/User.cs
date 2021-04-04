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
        [StringLength(100, MinimumLength = 3)]
        [RegularExpression("^[A-Za-z][A-Za-z0-9]*$", ErrorMessage = "The {0} field must start with a letter, and can only contain letters or numbers.")]
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
