using InternetId.Users.Data;
using InternetId.Users.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;

namespace InternetId.Server.Pages
{
    [Authorize]
    public class ProfileModel : PageModel
    {
        private readonly UserFinder userFinder;

        public ProfileModel(UserFinder userFinder)
        {
            this.userFinder = userFinder;
        }

        public User CurrentUser { get; private set; } = null!;

        public async Task OnGetAsync()
        {
            CurrentUser = (await userFinder.FindByClaimsPrincipalAsync(User))!;
        }
    }
}
