using InternetId.Credentials;
using InternetId.Server.Services;
using InternetId.Users.Data;
using InternetId.Users.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace InternetId.Server.Pages
{
    [AllowAnonymous]
    public class ChangePasswordModel : PageModel
    {
        private readonly ILogger<ChangePasswordModel> logger;
        private readonly SignInManager signInManager;
        private readonly UserFinder userManager;
        private readonly UserPasswordService passwordService;

        public ChangePasswordModel(
            ILogger<ChangePasswordModel> logger,
            SignInManager signInManager,
            UserFinder userManager,
            UserPasswordService passwordService,
            UserVerifyEmailService verifyEmailService)
        {
            this.logger = logger;
            this.signInManager = signInManager;
            this.userManager = userManager;
            this.passwordService = passwordService;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public string? ReturnUrl { get; set; }

        public class InputModel
        {
            [Required]
            [Display(Name = "Username or Email")]
            public string? Login { get; set; }

            [Required]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 9)]
            [DataType(DataType.Password)]
            [Display(Name = "Current password")]
            public string? CurrentPassword { get; set; }

            [Required]
            [DataType(DataType.Password)]
            [Display(Name = "New password")]
            public string? NewPassword { get; set; }

            [Required]
            [DataType(DataType.Password)]
            [Display(Name = "Confirm new password")]
            [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
            public string? ConfirmPassword { get; set; }
        }

        public void OnGet(string? returnUrl = null)
        {
            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            if (ModelState.IsValid)
            {
                var user = await userManager.FindAsync(Input.Login!);

                if (user != null)
                {
                    var result = await passwordService.VerifyPasswordAsync(user, Input.CurrentPassword!);

                    switch (result.Outcome)
                    {
                        case VerifySecretOutcome.Invalid:

                            ModelState.AddModelError(string.Empty, "Incorrect username/email or password.");
                            break;

                        case VerifySecretOutcome.LockedOut:

                            ModelState.AddModelError(string.Empty, result.Message ?? "Too many failed login attempts. The account is temporarily locked. Please try again later.");
                            break;

                        case VerifySecretOutcome.Expired:
                        case VerifySecretOutcome.Verified:

                            // Expired and valid passwords can be changed.
                            await passwordService.SetPasswordAsync(user, Input.NewPassword!);

                            // Before redirecting.
                            await signInManager.SignOutAsync();

                            return RedirectToPage("Login", new { login = Input.Login, returnUrl = returnUrl });

                        default:

                            throw new NotSupportedException($"The {result.Outcome} {typeof(VerifySecretOutcome)} is not supported.");
                    }
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Incorrect username/email.");
                }
            }

            ReturnUrl = returnUrl;

            return Page();
        }
    }
}
