﻿using System;
using System.ComponentModel.DataAnnotations;

namespace InternetId.Common.Codes
{
    public class Code
    {
        [Required]
        public string Purpose { get; set; } = null!;

        [Required]
        public string Key { get; set; } = null!;

        [Required(AllowEmptyStrings = true)]
        public string Data { get; set; } = string.Empty;

        public DateTimeOffset ValidUntil { get; set; }

        public int Attempts { get; set; }

        public DateTimeOffset? LockedOutUntil { get; set; }

        [Required]
        public string HashedCode { get; set; } = null!;
    }
}
