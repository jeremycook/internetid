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
    public class PasswordChangeModel : PageModel
    {
        private readonly ILogger<PasswordChangeModel> logger;
        private readonly SignInManager signInManager;
        private readonly UserFinder userFinder;
        private readonly PasswordService passwordService;

        public PasswordChangeModel(
            ILogger<PasswordChangeModel> logger,
            SignInManager signInManager,
            UserFinder userFinder,
            PasswordService passwordService)
        {
            this.logger = logger;
            this.signInManager = signInManager;
            this.userFinder = userFinder;
            this.passwordService = passwordService;
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
            [Display(Name = "Current password")]
            public string? CurrentPassword { get; set; }

            [Required]
            [MinLength(9, ErrorMessage = "The {0} must be at least {1} characters long.")]
            [DataType(DataType.Password)]
            [Display(Name = "New password")]
            public string? NewPassword { get; set; }

            [Required]
            [DataType(DataType.Password)]
            [Display(Name = "Confirm password")]
            [Compare(nameof(NewPassword), ErrorMessage = "The new password and confirmation password do not match.")]
            public string? ConfirmPassword { get; set; }
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
                        var result = await passwordService.VerifyPasswordAsync(user, Input.CurrentPassword!);

                        switch (result.Outcome)
                        {
                            case VerifySecretOutcome.Invalid:

                                ModelState.AddModelError(string.Empty, "Incorrect username/email or password.");
                                break;

                            case VerifySecretOutcome.Locked:

                                ModelState.AddModelError(string.Empty, result.Message ?? "Too many failed attempts. The account is temporarily locked. Please try again later.");
                                break;

                            case VerifySecretOutcome.Expired:
                            case VerifySecretOutcome.Verified:

                                // Expired and valid passwords can be changed.
                                await passwordService.SetPasswordAsync(user, Input.NewPassword!);

                                // Before redirecting.
                                await signInManager.SignOutAsync();

                                return RedirectToPage("Login", new { identifier = Input.Identifier, returnUrl = returnUrl });

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
