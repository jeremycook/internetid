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
    public class RegisterModel : PageModel
    {
        private readonly ILogger<RegisterModel> logger;
        private readonly UsersDbContext usersDb;
        private readonly PasswordService userPasswordService;
        private readonly EmailService userVerifyEmailService;

        public RegisterModel(
            ILogger<RegisterModel> logger,
            UsersDbContext usersDb,
            PasswordService userPasswordService,
            EmailService userVerifyEmailService)
        {
            this.logger = logger;
            this.usersDb = usersDb;
            this.userPasswordService = userPasswordService;
            this.userVerifyEmailService = userVerifyEmailService;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public string? ReturnUrl { get; set; }

        public class InputModel
        {
            private string? username;

            [Required]
            [MinLength(5, ErrorMessage = "The {0} must be at least {1} characters long.")]
            [RegularExpression("^[a-zA-Z][a-zA-Z0-9]*$", ErrorMessage = "The {0} must start with a letter, and may only contain letters and numbers.")]
            [Display(Name = "Username")]
            public string? Username { get => username; set => username = value?.Trim(); }

            [Required]
            [MinLength(9, ErrorMessage = "The {0} must be at least {1} characters long.")]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string? Password { get; set; }

            [Required]
            [DataType(DataType.Password)]
            [Display(Name = "Confirm password")]
            [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
            public string? ConfirmPassword { get; set; }

            [Required]
            [Display(Name = "Display name")]
            public string? DisplayName { get; set; }

            [EmailAddress]
            [Display(Name = "Email")]
            public string? Email { get; set; }
        }

        public void OnGet(string? returnUrl = null)
        {
            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            try
            {
                if (Input.Username is string username && await usersDb.Users.AnyAsync(o => o.LowercaseUsername == username.ToLowerInvariant()))
                {
                    ModelState.AddModelError(string.Empty, $"The '{Input.Username}' username is taken. Consider appending numbers or trying a different username.");
                }

                if (ModelState.IsValid)
                {
                    // Use a transaction to avoid creating a user
                    // but setting the password fails.
                    using var tx = await usersDb.Database.BeginTransactionAsync();

                    var user = new User
                    {
                        Username = Input.Username!,
                        DisplayName = Input.DisplayName!,
                        Email = Input.Email,
                    };
                    usersDb.Users.Add(user);
                    await usersDb.SaveChangesAsync();

                    await userPasswordService.SetPasswordAsync(user, Input.Password!);

                    await tx.CommitAsync();

                    if (user.Email != null)
                    {
                        await userVerifyEmailService.SendVerificationCodeAsync(user);
                        return RedirectToPage("EmailVerification", new { identifier = user.Username, returnUrl = returnUrl });
                    }
                    else if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl) && returnUrl.StartsWith(Url.Page("Register")))
                    {
                        return LocalRedirect(returnUrl);
                    }
                    else
                    {
                        return RedirectToPage("Profile");
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
