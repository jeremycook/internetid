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
    public class LoginModel : PageModel
    {
        private readonly ILogger<LoginModel> logger;
        private readonly UserFinder userFinder;
        private readonly PasswordService passwordService;
        private readonly SignInManager signInManager;
        private readonly EmailService verifyEmailService;

        public LoginModel(
            ILogger<LoginModel> logger,
            SignInManager signInManager,
            UserFinder userFinder,
            PasswordService passwordService,
            EmailService verifyEmailService)
        {
            this.logger = logger;
            this.signInManager = signInManager;
            this.userFinder = userFinder;
            this.passwordService = passwordService;
            this.verifyEmailService = verifyEmailService;
        }

        public string? ReturnUrl { get; set; }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            [Required]
            [Display(Name = "Username or email")]
            public string? Identifier { get; set; }

            [Required]
            [DataType(DataType.Password)]
            public string? Password { get; set; }
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
                    var user = await userFinder.FindByIdentifierAsync(Input.Identifier!);

                    if (user != null)
                    {
                        var result = await passwordService.VerifyPasswordAsync(user, Input.Password!);

                        switch (result.Outcome)
                        {
                            case VerifySecretOutcome.Invalid:

                                ModelState.AddModelError(string.Empty, "Incorrect username/email or password.");
                                break;

                            case VerifySecretOutcome.Locked:

                                ModelState.AddModelError(string.Empty, result.Message ?? "Too many failed login attempts. The user account is temporarily locked out. Please try again later.");
                                break;

                            case VerifySecretOutcome.Expired:

                                return RedirectToPage("PasswordChange", new { identifier = Input.Identifier, returnUrl = returnUrl });

                            case VerifySecretOutcome.Verified:

                                await signInManager.RefreshSignInAsync(user);

                                if (!string.IsNullOrWhiteSpace(user.Email) && !user.EmailVerified)
                                {
                                    // Ask them to verify their email address.
                                    return RedirectToPage("EmailVerificationRequest", new { identifier = Input.Identifier, returnUrl = returnUrl });
                                }
                                else
                                {
                                    if (returnUrl != null && Url.IsLocalUrl(returnUrl) && !returnUrl.StartsWith(Url.Page("Login")))
                                    {
                                        return LocalRedirect(returnUrl);
                                    }
                                    else
                                    {
                                        return RedirectToPage("Profile");
                                    }
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
