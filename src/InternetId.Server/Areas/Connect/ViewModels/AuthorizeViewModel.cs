using System.ComponentModel.DataAnnotations;

namespace InternetId.Server.Areas.Connect.ViewModels
{
    public class AuthorizeViewModel
    {
        public AuthorizeViewModel(string applicationName, string scope)
        {
            ApplicationName = applicationName;
            Scope = scope;
        }

        [Display(Name = "Application")]
        public string ApplicationName { get; set; }

        [Display(Name = "Scope")]
        public string Scope { get; set; }
    }
}
