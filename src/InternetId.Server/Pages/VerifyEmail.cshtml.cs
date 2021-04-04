using InternetId.Server.Services;
using InternetId.Users.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace InternetId.Server.Pages
{
    [AllowAnonymous]
    public class VeriyEmailModel : PageModel
    {
        private readonly UserFinder userManager;
        private readonly SignInManager signInManager;
        private readonly UserVerifyEmailService verifyEmailService;

        public VeriyEmailModel(UserFinder userManager, SignInManager signInManager, UserVerifyEmailService verifyEmailService)
        {
            this.userManager = userManager;
            this.signInManager = signInManager;
            this.verifyEmailService = verifyEmailService;
        }

        public string? ReturnUrl { get; private set; }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            [Required]
            public string? Username { get; set; }

            [Required]
            public string? Code { get; set; }
        }

        public IActionResult OnGet(string username)
        {
            Input = new InputModel
            {
                Username = username,
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            if (ModelState.IsValid)
            {
                var user = await userManager.FindByUsernameAsync(Input.Username!);

                if (user == null)
                {
                    ModelState.AddModelError(string.Empty, "An account could not be found that matches that username.");
                }
                else
                {
                    var result = await verifyEmailService.VerifyEmailAsync(user, Input.Code!);
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

            ReturnUrl = returnUrl;

            return Page();
        }

        public async Task<IActionResult> OnPostRequestCodeAsync(string? returnUrl = null)
        {
            ModelState.ClearValidationState("Input.Code");

            if (ModelState.IsValid)
            {
                var user = await userManager.FindByUsernameAsync(Input.Username!);

                if (user == null)
                {
                    ModelState.AddModelError(string.Empty, "An account could not be found that matches that username.");
                }
                else if (string.IsNullOrWhiteSpace(user.Email))
                {
                    ModelState.AddModelError(string.Empty, "The account does not have an email address associated with it.");
                }
                else if (user.EmailVerified)
                {
                    ModelState.AddModelError(string.Empty, "The account's email has been verified.");
                }
                else
                {
                    await verifyEmailService.SendVerifyEmailCodeAsync(user);
                    return RedirectToPage("VerifyEmail", new { username = user.Username, returnUrl = returnUrl });
                }
            }

            ReturnUrl = returnUrl;

            return Page();
        }
    }
}
