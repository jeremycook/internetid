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
        private readonly UserFinder userManager;
        private readonly UserPasswordService passwordService;
        private readonly SignInManager signInManager;
        private readonly UserVerifyEmailService verifyEmailService;

        public LoginModel(
            ILogger<LoginModel> logger,
            SignInManager signInManager,
            UserFinder userManager,
            UserPasswordService passwordService,
            UserVerifyEmailService verifyEmailService)
        {
            this.logger = logger;
            this.signInManager = signInManager;
            this.userManager = userManager;
            this.passwordService = passwordService;
            this.verifyEmailService = verifyEmailService;
        }

        public string? ReturnUrl { get; set; }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            [Required]
            [Display(Name = "Username or Email")]
            public string? Login { get; set; }

            [Required]
            [DataType(DataType.Password)]
            public string? Password { get; set; }
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
                    var result = await passwordService.VerifyPasswordAsync(user, Input.Password!);

                    switch (result.Outcome)
                    {
                        case VerifySecretOutcome.Invalid:

                            ModelState.AddModelError(string.Empty, "Incorrect username/email or password.");
                            break;

                        case VerifySecretOutcome.LockedOut:

                            ModelState.AddModelError(string.Empty, result.Message ?? "Too many failed login attempts. The user account is temporarily locked out. Please try again later.");
                            break;

                        case VerifySecretOutcome.Expired:

                            return RedirectToPage("ChangePassword", new { username = user.Username, returnUrl = returnUrl });

                        case VerifySecretOutcome.Verified:

                            if (!await signInManager.CanSignInAsync(user))
                            {
                                ModelState.AddModelError(string.Empty, "Cannot sign in the user account.");
                                break;
                            }
                            else if (!string.IsNullOrWhiteSpace(user.Email) && !user.EmailVerified)
                            {
                                // Sign them in but keep asking them to verify their email address.
                                await signInManager.SignInAsync(user);

                                await verifyEmailService.SendVerifyEmailCodeAsync(user);
                                return RedirectToPage("VerifyEmail", new { username = user.Username, returnUrl = returnUrl });
                            }
                            else
                            {
                                await signInManager.SignInAsync(user);

                                returnUrl ??= Url.Page("Profile");

                                if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl) && returnUrl != Url.Page("Login"))
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

            ReturnUrl = returnUrl;

            return Page();
        }
    }
}
