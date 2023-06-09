﻿using InternetId.Users.Services;
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
    public class EmailVerificationRequestModel : PageModel
    {
        private readonly ILogger<EmailVerificationRequestModel> logger;
        private readonly UserFinder userFinder;
        private readonly EmailService emailService;

        public EmailVerificationRequestModel(
            ILogger<EmailVerificationRequestModel> logger,
            UserFinder userFinder,
            EmailService emailService)
        {
            this.logger = logger;
            this.userFinder = userFinder;
            this.emailService = emailService;
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
                            await emailService.SendVerificationCodeAsync(user);
                            return RedirectToPage("EmailVerification", new { identifier = Input.Identifier, returnUrl = returnUrl });
                        }
                        else
                        {
                            ModelState.AddModelError(string.Empty, "The account does not have an email address for sending a code to.");
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
