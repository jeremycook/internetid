using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using InternetId.Server.ViewModels.Shared;

namespace InternetId.Server.Controllers
{
    public class ErrorController : Controller
    {
        [Route("error")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            // If the error was not caused by an invalid
            // OIDC request, display a generic error page.
            var response = HttpContext.GetOpenIddictServerResponse();
            if (response == null)
            {
                return View();
            }

            return View(new ErrorViewModel
            {
                Error = response.Error,
                Description = response.ErrorDescription
            });
        }
    }
}