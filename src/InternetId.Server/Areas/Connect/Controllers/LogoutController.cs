using InternetId.Server.Services;
using InternetId.Users.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;

namespace InternetId.Server.Areas.Connect.Controllers
{
    [Area("Connect")]
    [Route("[area]/[controller]")]
    public class LogoutController : Controller
    {
        private readonly IOpenIddictApplicationManager applicationManager;
        private readonly IOpenIddictAuthorizationManager authorizationManager;
        private readonly IOpenIddictScopeManager scopeManager;
        private readonly UserFinder userFinder;
        private readonly UserClientManager userClientManager;
        private readonly SignInManager signInManager;
        private readonly ILogger<LogoutController> logger;

        public LogoutController(
            IOpenIddictApplicationManager applicationManager,
            IOpenIddictAuthorizationManager authorizationManager,
            IOpenIddictScopeManager scopeManager,
            UserFinder userFinder,
            UserClientManager userClientManager,
            SignInManager signInManager,
            ILogger<LogoutController> logger)
        {
            this.applicationManager = applicationManager;
            this.authorizationManager = authorizationManager;
            this.scopeManager = scopeManager;
            this.userFinder = userFinder;
            this.userClientManager = userClientManager;
            this.signInManager = signInManager;
            this.logger = logger;
        }

        [HttpGet]
        public IActionResult Logout()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName(nameof(Logout))]
        public IActionResult LogoutPost()
        {
            // Note: If any identity providers could be used to authenticate
            // (as opposed to username and password) then those local and
            // external cookies should be cleaned up at this point.

            // Returning a SignOutResult asks OpenIddict to redirect the user agent
            // to the postlogoutredirecturi specified by the client application or to
            // the RedirectUri specified in the authentication properties if none was set.
            logger.LogInformation("Logout");
            return SignOut(
                authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                properties: new AuthenticationProperties
                {
                    RedirectUri = "/"
                }
            );
        }
    }
}