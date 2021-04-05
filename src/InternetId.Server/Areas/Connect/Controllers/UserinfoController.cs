using InternetId.Users.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace InternetId.Server.Areas.Connect.Controllers
{
    [Area("Connect")]
    [Route("[area]/[controller]")]
    public class UserinfoController : Controller
    {
        // See:
        // https://openid.net/specs/openid-connect-core-1_0.html#ScopeClaims
        // http://openid.net/specs/openid-connect-core-1_0.html#StandardClaims
        private static readonly IDictionary<string, string[]> _scopeClaims = new Dictionary<string, string[]>
        {
            [Scopes.Profile] = new[] { "name", "family_name", "given_name", "middle_name", "nickname", "preferred_username", "profile", "picture", "website", "gender", "birthdate", "zoneinfo", "locale", "updated_at" },
            [Scopes.Address] = new[] { "address" },
            [Scopes.Email] = new[] { "email", "email_verified" },
            [Scopes.Phone] = new[] { "phone", "phone_verified" },
        };

        private readonly UserClientManager userClientManager;

        public UserinfoController(UserClientManager userClientManager)
        {
            this.userClientManager = userClientManager;
        }

        [Authorize(AuthenticationSchemes = OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)]
        [HttpGet, HttpPost, Produces("application/json")]
        public async Task<IActionResult> Userinfo()
        {
            var user = await userClientManager.FindByClientPrincipalAsync(User);
            if (user == null)
            {
                return Challenge(
                    authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                    properties: new AuthenticationProperties(new Dictionary<string, string?>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidToken,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The specified access token is bound to an account that no longer exists."
                    }));
            }

            // TODO: Eventually add other claims.
            var userClaims = new List<Claim>();

            var claims = new Dictionary<string, object>(StringComparer.Ordinal)
            {
                // The sub claim is a mandatory claim that must be included in the JSON response.
                [Claims.Subject] = User.FindFirstValue(Claims.Subject),
            };

            if (User.HasScope(Scopes.Email) && user.EmailVerified && !string.IsNullOrWhiteSpace(user.Email))
            {
                // Only pass the email if it has been verified.
                claims[Claims.Email] = user.Email;
                claims[Claims.EmailVerified] = user.EmailVerified ? "true" : "false";
            }

            if (User.HasScope(Scopes.Profile))
            {
                claims[Claims.PreferredUsername] = user.Username;
                claims[Claims.Name] = user.DisplayName;

                var profileClaims = _scopeClaims[Scopes.Profile];
                foreach (var claim in userClaims.Where(o => profileClaims.Contains(o.Type)))
                {
                    claims.TryAdd(claim.Type, claim.Value);
                }
            }

            if (User.HasScope(Scopes.Address))
            {
                var addressClaims = _scopeClaims[Scopes.Address];
                foreach (var claim in userClaims.Where(o => addressClaims.Contains(o.Type)))
                {
                    claims.Add(claim.Type, claim.Value);
                }
            }

            if (User.HasScope(Scopes.Phone))
            {
                var phoneClaims = _scopeClaims[Scopes.Phone];
                foreach (var claim in userClaims.Where(o => phoneClaims.Contains(o.Type)))
                {
                    claims.Add(claim.Type, claim.Value);
                }
            }

            if (User.HasScope(Scopes.Roles))
            {
                // Intentionally ignoring the roles scope until we have a way to manage that per client.
            }

            return Ok(claims);
        }
    }
}
