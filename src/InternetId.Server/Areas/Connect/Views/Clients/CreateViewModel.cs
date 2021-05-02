using OpenIddict.Abstractions;
using System;
using System.ComponentModel.DataAnnotations;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace InternetId.Server.Areas.Connect.Views
{
    public class CreateViewModel
    {
        [Required]
        [StringLength(50)]
        [Display(Name = "Name")]
        public string? Name { get; set; }

        [Required]
        [RegularExpression("^https://.+", ErrorMessage = "The {0} must begin with \"https://\".")]
        [Display(Name = "Redirect URL")]
        public string? RedirectUrl { get; set; }

        [Required]
        [RegularExpression("^https://.+", ErrorMessage = "The {0} must begin with \"https://\".")]
        [Display(Name = "Post logout return URL")]
        public string? PostLogoutRedirectUrl { get; set; }

        /// <summary>
        /// Create a new <see cref="OpenIddictApplicationDescriptor"/> based on <c>this</c>.
        /// </summary>
        /// <returns></returns>
        public OpenIddictApplicationDescriptor ToOpenIddictApplicationDescriptor()
        {
            return new OpenIddictApplicationDescriptor
            {
                DisplayName = Name,
                RedirectUris = { new Uri(RedirectUrl!) },
                PostLogoutRedirectUris = { new Uri(PostLogoutRedirectUrl!) },
                ClientId = Guid.NewGuid().ToString(),
                ClientSecret = Guid.NewGuid().ToString(),
                ConsentType = ConsentTypes.Explicit,
                Permissions =
                {
                    Permissions.Endpoints.Authorization,
                    Permissions.Endpoints.Logout,
                    Permissions.Endpoints.Token,
                    Permissions.GrantTypes.AuthorizationCode,
                    Permissions.GrantTypes.RefreshToken,
                    Permissions.ResponseTypes.Code,
                    Permissions.Scopes.Email,
                    Permissions.Scopes.Profile,
                },
                Requirements =
                {
                    Requirements.Features.ProofKeyForCodeExchange
                }
            };
        }
    }
}
