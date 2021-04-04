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

        public string? ReturnUrl { get; private set; }

        [BindProperty]
        public InputModel Input { get; set; } = new();

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
                        if (result.Outcome == Credentials.VerifySecretOutcome.Verified)
                        {
                            await signInManager.SignInAsync(user);

                            if (string.IsNullOrWhiteSpace(returnUrl) || !Url.IsLocalUrl(returnUrl))
                            {
                                returnUrl = Url.Page("Profile");
                            }
                            return LocalRedirect(returnUrl);
                        }
                        else
                        {
                            ModelState.AddModelError(string.Empty, result.Message);
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

        public async Task<IActionResult> OnPostRequestCodeAsync(string? returnUrl = null)
        {
            ModelState.ClearValidationState("Input.Code");

            if (ModelState.IsValid)
            {
                var user = await userFinder.FindByIdentifierAsync(Input.Identifier!);

                if (user == null)
                {
                    ModelState.AddModelError(string.Empty, "Incorrect username/email.");
                }
                else if (string.IsNullOrWhiteSpace(user.Email))
                {
                    ModelState.AddModelError(string.Empty, "The account does not have an email address for sending a verification code to.");
                }
                else if (user.EmailVerified)
                {
                    ModelState.AddModelError(string.Empty, "The account email has already been verified.");
                }
                else
                {
                    await verifyEmailService.SendVerificationCodeAsync(user);
                    return RedirectToPage("EmailVerification", new { identifier = Input.Identifier!, returnUrl = returnUrl });
                }
            }

            ReturnUrl = returnUrl;

            return Page();
        }
    }
}
