using InternetId.Users.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using System;
using System.Collections.Generic;
using System.Linq;
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

        private readonly UserManager<User> _userManager;

        public UserinfoController(UserManager<User> userManager)
            => _userManager = userManager;

        [Authorize(AuthenticationSchemes = OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)]
        [HttpGet("~/connect/userinfo"), HttpPost("~/connect/userinfo"), Produces("application/json")]
        public async Task<IActionResult> Userinfo()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge(
                    authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                    properties: new AuthenticationProperties(new Dictionary<string, string>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidToken,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
                            "The specified access token is bound to an account that no longer exists."
                    }));
            }

            var userClaims = await _userManager.GetClaimsAsync(user);

            var claims = new Dictionary<string, object>(StringComparer.Ordinal)
            {
                // The sub claim is a mandatory claim that must be included in the JSON response.
                [Claims.Subject] = await _userManager.GetUserIdAsync(user)
            };

            if (User.HasScope(Scopes.Profile))
            {
                var profileClaims = _scopeClaims[Scopes.Profile];
                foreach (var claim in userClaims.Where(o => profileClaims.Contains(o.Type)))
                {
                    claims.Add(claim.Type, claims.Values);
                }
            }

            if (User.HasScope(Scopes.Address))
            {
                var addressClaims = _scopeClaims[Scopes.Address];
                foreach (var claim in userClaims.Where(o => addressClaims.Contains(o.Type)))
                {
                    claims.Add(claim.Type, claims.Values);
                }
            }

            if (User.HasScope(Scopes.Email))
            {
                claims[Claims.Email] = await _userManager.GetEmailAsync(user);
                claims[Claims.EmailVerified] = await _userManager.IsEmailConfirmedAsync(user);
            }

            if (User.HasScope(Scopes.Phone))
            {
                claims[Claims.PhoneNumber] = await _userManager.GetPhoneNumberAsync(user);
                claims[Claims.PhoneNumberVerified] = await _userManager.IsPhoneNumberConfirmedAsync(user);
            }

            // Intentionally ignoring the roles scope until we have an interface for managing that per client.

            return Ok(claims);
        }
    }
}
