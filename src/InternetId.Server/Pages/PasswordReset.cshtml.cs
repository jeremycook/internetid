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
    public class PasswordResetModel : PageModel
    {
        private readonly ILogger<PasswordResetModel> logger;
        private readonly SignInManager signInManager;
        private readonly UserFinder userFinder;
        private readonly PasswordResetService passwordResetService;

        public PasswordResetModel(
            ILogger<PasswordResetModel> logger,
            SignInManager signInManager,
            UserFinder userFinder,
            PasswordResetService passwordResetService)
        {
            this.logger = logger;
            this.signInManager = signInManager;
            this.userFinder = userFinder;
            this.passwordResetService = passwordResetService;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public string? ReturnUrl { get; set; }

        public class InputModel
        {
            [Required]
            [Display(Name = "Username or email")]
            public string? Identifier { get; set; }
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
                        if (!string.IsNullOrWhiteSpace(user.Email))
                        {
                            await passwordResetService.SendPasswordResetCodeAsync(user);

                            return RedirectToPage("PasswordResetVerification", new { identifier = Input.Identifier, returnUrl = returnUrl });
                        }
                        else
                        {
                            ModelState.AddModelError(string.Empty, "The user does not have an email address for sending a password reset code to.");
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
