using InternetId.Server.Areas.Connect.Views;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OpenIddict.Abstractions;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace InternetId.Server.Areas.Connect.Controllers
{
    [Authorize]
    [Area("Connect")]
    [Route("[area]/[controller]")]
    public class ClientsController : Controller
    {
        private readonly ILogger<ClientsController> logger;
        private readonly IOpenIddictApplicationManager openIddictApplicationManager;

        public ClientsController(
            ILogger<ClientsController> logger,
            IOpenIddictApplicationManager openIddictApplicationManager)
        {
            this.logger = logger;
            this.openIddictApplicationManager = openIddictApplicationManager;
        }

        [HttpGet("[action]")]
        public ActionResult Create()
        {
            var input = new CreateViewModel();
            return View(input);
        }

        [HttpPost("[action]")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create([Required] CreateViewModel input)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var application = input.ToOpenIddictApplicationDescriptor();

                    var result = await openIddictApplicationManager.CreateAsync(application);
                    logger.LogError("{User} created the {ClientId} OpenID Connect client", User.Identity!.Name, application.ClientId);

                    return View("Created", model: application);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, $"Suppressed {ex.GetType()}: {ex.Message}");
                    ModelState.AddModelError("", "An error occurred, and your changes were not saved.");
                }
            }

            return View(input);
        }
    }
}