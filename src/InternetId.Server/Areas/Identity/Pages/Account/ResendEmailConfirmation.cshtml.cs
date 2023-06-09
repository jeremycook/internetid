﻿using InternetId.Users.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace InternetId.Server.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class ResendEmailConfirmationModel : PageModel
    {
        private readonly UserManager<User> _userManager;
        private readonly IEmailSender _emailSender;

        public ResendEmailConfirmationModel(UserManager<User> userManager, IEmailSender emailSender)
        {
            _userManager = userManager;
            _emailSender = emailSender;
        }

        public string ReturnUrl { get; private set; }
        [BindProperty]
        public InputModel Input { get; set; }

        public string Message { get; set; }

        public class InputModel
        {
            [Required]
            [Display(Name = "Username or Email")]
            public string UsernameOrEmail { get; set; }
        }

        public void OnGet(string usernameOrEmail = null, string returnUrl = null)
        {
            ReturnUrl = returnUrl;
            Input = new InputModel
            {
                UsernameOrEmail = usernameOrEmail
            };
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            ReturnUrl = returnUrl;

            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = await _userManager.FindByNameOrEmailAsync(Input.UsernameOrEmail);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid username or email.");
                return Page();
            }

            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);

            var callbackUrl = Url.Page(
                "/Account/ConfirmEmail",
                pageHandler: null,
                values: new { userId = user.Id, code = code, returnUrl = returnUrl },
                protocol: Request.Scheme);
            await _emailSender.SendEmailAsync(
                user.Email,
                "Confirm your email",
                $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");

            return RedirectToPage("RegisterConfirmation", new { email = user.Email, returnUrl = returnUrl });
        }
    }
}
