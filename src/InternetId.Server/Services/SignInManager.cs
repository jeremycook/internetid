using InternetId.Users.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace InternetId.Server.Services
{
    public class SignInManager
    {
        public const string AuthenticationScheme = CookieAuthenticationDefaults.AuthenticationScheme;

        private readonly IHttpContextAccessor httpContextAccessor;

        public SignInManager(IHttpContextAccessor httpContextAccessor)
        {
            this.httpContextAccessor = httpContextAccessor;
        }

        public Task<ClaimsPrincipal> CreatePrincipalAsync(User user)
        {
            var claims = new List<Claim>
            {
                new Claim("sub", user.Id.ToString()),
                new Claim("username", user.Username),
                new Claim("name", user.DisplayName),
            };

            var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, AuthenticationScheme, "username", "role"));

            return Task.FromResult(principal);
        }

        public Task<bool> CanSignInAsync(User user)
        {
            // TODO: Set value accordingly once users can be disabled.

            return Task.FromResult(true);
        }

        /// <summary>
        /// Sign the <paramref name="user"/> into the <see cref="IHttpContextAccessor.HttpContext"/>.
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">The <see cref="IHttpContextAccessor.HttpContext"/> is unavailable.</exception>
        public async Task SignInAsync(User user)
        {
            if (!await CanSignInAsync(user))
            {
                throw new ArgumentException($"The user cannot be signed in.", nameof(user));
            }

            var httpContext =
                httpContextAccessor.HttpContext ??
                throw new InvalidOperationException("Unable to sign in the user. The IHttpContextAccessor.HttpContext is unavailable.");

            var principal = await CreatePrincipalAsync(user);

            var authenticationProperties = new AuthenticationProperties
            {
                IsPersistent = false, // match browser session
            };

            await httpContext.SignInAsync(scheme: AuthenticationScheme, principal, authenticationProperties);
        }

        /// <summary>
        /// Sign the principal out of the <see cref="IHttpContextAccessor.HttpContext"/>.
        /// </summary>
        /// <returns></returns>
        public async Task SignOutAsync()
        {
            var httpContext =
                httpContextAccessor.HttpContext ??
                throw new InvalidOperationException("Unable to sign in the user. The IHttpContextAccessor.HttpContext is unavailable.");

            await httpContext.SignOutAsync(AuthenticationScheme);
        }

        public async Task RefreshSignInAsync(User user)
        {
            await SignOutAsync();
            await SignInAsync(user);
        }
    }
}