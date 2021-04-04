using InternetId.Server.Services;
using InternetId.Users.Data;
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
    [Authorize]
    public class ProfileEditorModel : PageModel
    {
        private readonly ILogger<ProfileEditorModel> logger;
        private readonly SignInManager signInManager;
        private readonly UsersDbContext usersDb;
        private readonly UserFinder userFinder;

        public ProfileEditorModel(
            ILogger<ProfileEditorModel> logger,
            SignInManager signInManager,
            UsersDbContext usersDb,
            UserFinder userFinder)
        {
            this.logger = logger;
            this.signInManager = signInManager;
            this.usersDb = usersDb;
            this.userFinder = userFinder;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            [Required]
            [Display(Name = "Display name")]
            public string? DisplayName { get; set; }
        }

        public async void OnGetAsync()
        {
            var user =
                await userFinder.FindByClaimsPrincipalAsync(User) ??
                throw new InvalidOperationException("User not found");

            Input.DisplayName = user.DisplayName;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user =
                await userFinder.FindByClaimsPrincipalAsync(User) ??
                throw new InvalidOperationException("User not found");

            try
            {
                if (ModelState.IsValid)
                {
                    user.DisplayName = Input.DisplayName!;
                    var changedRecords = await usersDb.SaveChangesAsync();

                    if (changedRecords > 0)
                    {
                        await signInManager.SignOutAsync();
                        await signInManager.SignInAsync(user);
                    }

                    return RedirectToPage("Profile");
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

            return Page();
        }
    }
}
