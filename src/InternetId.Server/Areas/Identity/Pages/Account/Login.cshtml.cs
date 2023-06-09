﻿using InternetId.Users.Data;
using InternetId.Users.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace InternetId.Server.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class LoginModel : PageModel
    {
        private readonly UserManager<User> _userManager;
        private readonly VerificationService _verifyEmailService;
        private readonly SignInManager<User> _signInManager;
        private readonly ILogger<LoginModel> _logger;

        public LoginModel(SignInManager<User> signInManager,
            ILogger<LoginModel> logger,
            UserManager<User> userManager,
            VerificationService verifyEmailService)
        {
            _userManager = userManager;
            _verifyEmailService = verifyEmailService;
            _signInManager = signInManager;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        public string ReturnUrl { get; set; }

        [TempData]
        public string ErrorMessage { get; set; }

        public class InputModel
        {
            [Required]
            [Display(Name = "Username or Email")]
            public string UsernameOrEmail { get; set; }

            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; }

            [Display(Name = "Remember me?")]
            public bool RememberMe { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }

            returnUrl ??= Url.Content("~/");

            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Page("Manage/Index");

            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByNameOrEmailAsync(Input.UsernameOrEmail);

                if (user != null)
                {
                    var result = await _signInManager.PasswordSignInAsync(user, Input.Password, Input.RememberMe, lockoutOnFailure: true);
                    if (result.Succeeded)
                    {
                        _logger.LogInformation("User logged in.");
                        return LocalRedirect(returnUrl);
                    }
                    if (result.RequiresTwoFactor)
                    {
                        return RedirectToPage("LoginWith2fa", new { returnUrl = returnUrl, rememberMe = Input.RememberMe });
                    }
                    if (result.IsLockedOut)
                    {
                        _logger.LogWarning("User account locked out.");
                        return RedirectToPage("Lockout");
                    }

                    if (_userManager.Options.SignIn.RequireConfirmedAccount && !user.EmailConfirmed)
                    {
                        await _verifyEmailService.SendVerifyEmailCodeAsync(user, user.Email);
                        return RedirectToPage("EmailVerification", new { username = user.UserName, returnUrl = returnUrl });
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty, "Invalid username/email or password.");
                    }
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Invalid username/email.");
                }
            }

            // If we got this far, something failed, redisplay form
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
            ReturnUrl = returnUrl;
            return Page();
        }
    }
}
