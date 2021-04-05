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
    [Authorize]
    public class ProfileManagerModel : PageModel
    {
        private readonly ILogger<ProfileManagerModel> logger;
        private readonly SignInManager signInManager;
        private readonly UsersDbContext usersDb;
        private readonly UserFinder userFinder;

        public ProfileManagerModel(
            ILogger<ProfileManagerModel> logger,
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
            [Display(Name = "Name")]
            public string? Name { get; set; }
        }

        public async Task<ActionResult> OnGetAsync()
        {
            if (await userFinder.FindByLocalPrincipalAsync(User) is not User user)
            {
                return NotFound();
            }

            Input.Name = user.Name;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                if (await userFinder.FindByLocalPrincipalAsync(User) is not User user)
                {
                    return NotFound();
                }

                if (ModelState.IsValid)
                {
                    user.Name = Input.Name;
                    var changes = await usersDb.SaveChangesAsync();

                    if (changes > 0)
                    {
                        await signInManager.RefreshSignInAsync(user);
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
