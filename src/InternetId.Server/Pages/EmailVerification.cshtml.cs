using InternetId.Credentials;
using InternetId.Server.Services;
using InternetId.Users.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace InternetId.Server.Pages
{
    [AllowAnonymous]
    public class EmailVerificationModel : PageModel
    {
        private readonly ILogger<EmailVerificationModel> logger;
        private readonly UserFinder userFinder;
        private readonly SignInManager signInManager;
        private readonly EmailService verifyEmailService;

        public EmailVerificationModel(ILogger<EmailVerificationModel> logger, UserFinder userFinder, SignInManager signInManager, EmailService verifyEmailService)
        {
            this.logger = logger;
            this.userFinder = userFinder;
            this.signInManager = signInManager;
            this.verifyEmailService = verifyEmailService;
        }


        [BindProperty]
        public InputModel Input { get; set; } = new();
        public string? ReturnUrl { get; private set; }
        public bool ShowRequestCodeButton { get; private set; } = false;

        public class InputModel
        {
            [Required]
            [Display(Name = "Username or email")]
            public string? Identifier { get; set; }

            [Required]
            public string? Code { get; set; }
        }

        public void OnGet(string? identifier = null, string? returnUrl = null)
        {
            Input.Identifier = identifier;
            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var user = await userFinder.FindByUsernameAsync(Input.Identifier!);

                    if (user == null)
                    {
                        ModelState.AddModelError(string.Empty, "Incorrect username/email.");
                    }
                    else
                    {
                        var result = await verifyEmailService.VerifyAsync(user, Input.Code!);

                        switch (result.Outcome)
                        {
                            case VerifySecretOutcome.Invalid:
                            case VerifySecretOutcome.Expired:

                                ModelState.AddModelError(string.Empty, "The code is incorrect or has expired. If you think you entered it correctly then try requesting a new code.");
                                ShowRequestCodeButton = true;
                                break;

                            case VerifySecretOutcome.Locked:

                                ModelState.AddModelError(string.Empty, result.Message ?? "Too many failed attempts. Reset attempts are temporarily locked. Please try again later.");
                                break;

                            case VerifySecretOutcome.Verified:

                                // Should already be logged in, just continue.
                                if (Url.IsLocalUrl(returnUrl))
                                {
                                    return LocalRedirect(returnUrl);
                                }
                                else
                                {
                                    return RedirectToPage("Profile");
                                }

                            default:

                                throw new NotSupportedException($"The {result.Outcome} {typeof(VerifySecretOutcome)} is not supported.");
                        }
                    }
                }
            }
            catch (ValidationException ex)
            {
                logger.LogWarning(ex, $"Suppressed {ex.GetType()}: {ex.Message}");
                ModelState.AddModelError(string.Empty, ex.Message);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Suppressed {ex.GetType()}: {ex.Message}");
                ModelState.AddModelError(string.Empty, "An unexpected error occurred.");
            }

            ReturnUrl = returnUrl;

            return Page();
        }
    }
}
