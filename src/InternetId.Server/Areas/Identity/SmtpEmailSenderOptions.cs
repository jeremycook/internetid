﻿#nullable enable
namespace InternetId.Server.Areas.Identity
{
    public class SmtpEmailSenderOptions
    {
        public string Host { get; set; } = null!;
        public int? Port { get; set; }
        public bool EnableSsl { get; set; } = true;
        public string? Username { get; set; }
        public string? Password { get; set; }
    }
}