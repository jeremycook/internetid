using InternetId.Credentials;
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
    public class PasswordResetVerificationModel : PageModel
    {
        private readonly ILogger<PasswordResetVerificationModel> logger;
        private readonly UserFinder userFinder;
        private readonly PasswordResetService passwordResetService;

        public PasswordResetVerificationModel(
            ILogger<PasswordResetVerificationModel> logger,
            UserFinder userFinder,
            PasswordResetService passwordResetService)
        {
            this.logger = logger;
            this.userFinder = userFinder;
            this.passwordResetService = passwordResetService;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public string? ReturnUrl { get; set; }
        public bool ShowRequestCodeButton { get; set; } = false;

        public class InputModel
        {
            [Required]
            [Display(Name = "Username or email")]
            public string? Identifier { get; set; }

            [Required]
            public string? Code { get; set; }

            [Required]
            [DataType(DataType.Password)]
            [MinLength(9, ErrorMessage = "The {0} must be at least {1} characters long.")]
            [Display(Name = "New password")]
            public string? NewPassword { get; set; }

            [Required]
            [DataType(DataType.Password)]
            [Compare(nameof(NewPassword), ErrorMessage = "The new password and confirmation password do not match.")]
            [Display(Name = "Confirm password")]
            public string? ConfirmPassword { get; set; }
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
                        var result = await passwordResetService.ResetPasswordAsync(user, Input.Code!, Input.NewPassword!);

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
