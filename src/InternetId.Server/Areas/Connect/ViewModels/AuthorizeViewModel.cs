﻿using System.ComponentModel.DataAnnotations;

namespace InternetId.Server.Areas.Connect.ViewModels
{
    public class AuthorizeViewModel
    {
        [Display(Name = "Application")]
        public string ApplicationName { get; set; }

        [Display(Name = "Scope")]
        public string Scope { get; set; }
    }
}