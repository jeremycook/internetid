using System;
using System.ComponentModel.DataAnnotations;

namespace InternetId.Credentials
{
    public class Credential
    {
        [Required]
        public string Purpose { get; set; } = null!;

        [Required]
        public string Key { get; set; } = null!;

        public int Attempts { get; set; }

        public DateTimeOffset? LockedUntil { get; set; }

        [Required(AllowEmptyStrings = true)]
        public string Data { get; set; } = string.Empty;

        [Required]
        public string Hash { get; set; } = null!;
    }
}
