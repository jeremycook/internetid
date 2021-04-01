using InternetId.Users.Data;
using InternetId.Users.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace InternetId.Server.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class EmailVerificationModel : PageModel
    {
        private readonly UserManager<User> userManager;
        private readonly SignInManager<User> signInManager;
        private readonly VerificationService verifyEmailService;

        public EmailVerificationModel(UserManager<User> userManager, SignInManager<User> signInManager, VerificationService verifyEmailService)
        {
            this.userManager = userManager;
            this.signInManager = signInManager;
            this.verifyEmailService = verifyEmailService;
        }

        public string ReturnUrl { get; private set; }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Required]
            public string Username { get; set; }

            [Required]
            public string Code { get; set; }
        }

        public IActionResult OnGet(string username)
        {
            Input = new InputModel
            {
                Username = username,
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            if (ModelState.IsValid)
            {
                var user = await userManager.FindByNameAsync(Input.Username);

                if (user == null)
                {
                    ModelState.AddModelError(string.Empty, "Invalid username.");
                }
                else
                {
                    var result = await verifyEmailService.VerifyEmailAsync(user, Input.Code);
                    if (result.IsValid)
                    {
                        await signInManager.SignInAsync(user, isPersistent: false);

                        if (string.IsNullOrWhiteSpace(returnUrl) || returnUrl == Url.Content("~/"))
                        {
                            returnUrl = Url.Page("Manage/Index");
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
    }
}
