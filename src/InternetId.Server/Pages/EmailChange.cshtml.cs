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
    public class EmailChangeModel : PageModel
    {
        private readonly ILogger<EmailChangeModel> logger;
        private readonly SignInManager signInManager;
        private readonly UserFinder userFinder;
        private readonly PasswordService passwordService;
        private readonly EmailService emailService;

        public EmailChangeModel(
            ILogger<EmailChangeModel> logger,
            SignInManager signInManager,
            UserFinder userFinder,
            PasswordService passwordService,
            EmailService emailService)
        {
            this.logger = logger;
            this.signInManager = signInManager;
            this.userFinder = userFinder;
            this.passwordService = passwordService;
            this.emailService = emailService;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public string? ReturnUrl { get; set; }

        public class InputModel
        {
            [Required]
            [Display(Name = "Username or email")]
            public string? Identifier { get; set; }

            [Required]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string? Password { get; set; }

            [Required]
            [DataType(DataType.EmailAddress)]
            [Display(Name = "New email")]
            public string? NewEmail { get; set; }
        }

        public async Task OnGetAsync(string? identifier = null, string? returnUrl = null)
        {
            Input.Identifier = identifier ?? (await userFinder.FindByLocalPrincipalAsync(User))?.Username;
            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var user = await userFinder.FindByIdentifierAsync(Input.Identifier!);

                    if (user != null)
                    {
                        var result = await passwordService.VerifyPasswordAsync(user, Input.Password!);

                        switch (result.Outcome)
                        {
                            case VerifySecretOutcome.Invalid:
                            case VerifySecretOutcome.Expired:

                                ModelState.AddModelError(string.Empty, "Incorrect username/email or password.");
                                break;

                            case VerifySecretOutcome.Locked:

                                ModelState.AddModelError(string.Empty, result.Message ?? "Too many failed attempts. The account is temporarily locked. Please try again later.");
                                break;

                            case VerifySecretOutcome.Verified:

                                bool verificationNeeded = await emailService.ChangeEmailAsync(user, Input.NewEmail!);

                                await signInManager.RefreshSignInAsync(user);

                                if (verificationNeeded)
                                {
                                    return RedirectToPage("EmailVerification", new { identifier = Input.Identifier, returnUrl = returnUrl });
                                }
                                else if (Url.IsLocalUrl(returnUrl))
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
                    else
                    {
                        ModelState.AddModelError(string.Empty, "Incorrect username/email.");
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
