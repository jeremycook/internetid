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

        private readonly UserFinder userFinder;

        public UserinfoController(UserFinder userFinder)
            => this.userFinder = userFinder;

        [Authorize(AuthenticationSchemes = OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)]
        [HttpGet("~/connect/userinfo"), HttpPost("~/connect/userinfo"), Produces("application/json")]
        public async Task<IActionResult> Userinfo()
        {
            var user = await userFinder.FindByClaimsPrincipalAsync(User);
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

            // TODO: Add claims from the user once we start tracking user claims.
            var userClaims = new List<Claim>();

            var claims = new Dictionary<string, object>(StringComparer.Ordinal)
            {
                // The sub claim is a mandatory claim that must be included in the JSON response.
                // TODO: Make the Subject per client by hashing the client ID and user ID together.
                [Claims.Subject] = user.Id.ToString(),
            };

            if (User.HasScope(Scopes.Email) && user.EmailVerified && !string.IsNullOrWhiteSpace(user.Email))
            {
                // Only pass the email if it has been verified.
                claims[Claims.Email] = user.Email;
                claims[Claims.EmailVerified] = user.EmailVerified;
            }

            if (User.HasScope(Scopes.Profile))
            {
                var profileClaims = _scopeClaims[Scopes.Profile];
                foreach (var claim in userClaims.Where(o => profileClaims.Contains(o.Type)))
                {
                    claims.Add(claim.Type, claim.Value);
                }

                if (!claims.ContainsKey(Claims.PreferredUsername))
                {
                    claims.Add(Claims.PreferredUsername, user.Username);
                }

                if (!claims.ContainsKey(Claims.Name))
                {
                    claims.Add(Claims.Name, user.DisplayName);
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
                // TODO: We don't track phone number right now.
            }

            if (User.HasScope(Scopes.Roles))
            {
                // Intentionally ignoring the roles scope until we have an interface for managing that per client.
            }

            return Ok(claims);
        }
    }
}
