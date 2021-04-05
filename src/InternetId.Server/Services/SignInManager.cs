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
        private readonly UsersDbContext usersDb;

        public SignInManager(IHttpContextAccessor httpContextAccessor, UsersDbContext usersDb)
        {
            this.httpContextAccessor = httpContextAccessor;
            this.usersDb = usersDb;
        }

        public Task<ClaimsPrincipal> CreateLocalPrincipalAsync(User user)
        {
            var claims = new List<Claim>
            {
                new Claim("sub", user.Id.ToString()),
                new Claim("username", user.Username),
                new Claim("name", user.DisplayName),
            };

            if (user.EmailVerified && !string.IsNullOrWhiteSpace(user.Email))
            {
                claims.Add(new Claim("email", user.Email));
                claims.Add(new Claim("email_verified", "true"));
            }

            // TODO: Provide a hook for contributing/modifying claims.

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

            var principal = await CreateLocalPrincipalAsync(user);

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