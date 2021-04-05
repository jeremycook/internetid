using InternetId.Users.Services;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace InternetId.Server.Areas.Connect.Controllers
{
    [Area("Connect")]
    [Route("[area]/[controller]")]
    public class TokenController : Controller
    {
        private readonly UserClientManager userClientManager;
        private readonly ILogger<TokenController> logger;

        public TokenController(
            UserClientManager userClientManager,
            ILogger<TokenController> logger)
        {
            this.userClientManager = userClientManager;
            this.logger = logger;
        }

        [HttpPost]
        [Produces("application/json")]
        public async Task<IActionResult> Token()
        {
            var request = HttpContext.GetOpenIddictServerRequest() ??
                throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

            if (request.IsAuthorizationCodeGrantType() || request.IsRefreshTokenGrantType())
            {
                // Retrieve the claims principal stored in the authorization code/device code/refresh token.
                var clientPrincipal =
                    (await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)).Principal ??
                    throw new InvalidOperationException("The client principal details cannot be retrieved.");

                // Retrieve the user profile corresponding to the authorization code/refresh token.
                // TODO: Invalidate the authorization code/refresh token
                // when any security attributes have changed (password, roles, etc.),
                // or if the user cannot sign in.
                var user = await userClientManager.FindByClientPrincipalAsync(clientPrincipal);
                if (user == null)
                {
                    logger.LogInformation("Invalid token");
                    return Forbid(
                        authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                        properties: new AuthenticationProperties(new Dictionary<string, string?>
                        {
                            [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
                            [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The token is no longer valid."
                        }));
                }

                foreach (var claim in clientPrincipal.Claims)
                {
                    claim.SetDestinations(userClientManager.GetDestinations(claim, clientPrincipal));
                }

                // Returning a SignInResult will ask OpenIddict to issue the appropriate access/identity tokens.
                logger.LogInformation("Valid token");
                return SignIn(clientPrincipal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }

            throw new InvalidOperationException("The specified grant type is not supported.");
        }
    }
}