using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;

namespace InternetId.MvcClient
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCookiePolicy(options => options.MinimumSameSitePolicy = SameSiteMode.Strict);

            services.AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            })

            .AddCookie(options =>
            {
                options.LoginPath = "/login";
            })

            .AddOpenIdConnect(options =>
            {
                // Note: these settings must match the application details
                // inserted in the database at the server level.
                options.ClientId = "mvc";
                options.ClientSecret = "901564A5-E7FE-42CB-B10D-61EF6A8F3654";

                options.RequireHttpsMetadata = false; // Should be true in production
                options.GetClaimsFromUserInfoEndpoint = true;
                options.SaveTokens = true;

                // Use the authorization code flow.
                options.ResponseType = OpenIdConnectResponseType.Code;
                options.AuthenticationMethod = OpenIdConnectRedirectBehavior.RedirectGet;

                // Note: setting the Authority allows the OIDC client middleware to automatically
                // retrieve the identity provider's configuration and spare you from setting
                // the different endpoints URIs or the token validation parameters explicitly.
                options.Authority = "https://localhost:44313/";

                options.Scope.Add("email");
                options.Scope.Add("roles");

                options.SecurityTokenValidator = new JwtSecurityTokenHandler
                {
                    // Disable the built-in JWT claims mapping feature.
                    InboundClaimTypeMap = new Dictionary<string, string>()
                };

                // WARNGING: This accepts all claims as-is!
                options.ClaimActions.MapAll();
                // Alternatively...
                // options.ClaimActions.MapAllExcept("iss", "nbf", "exp", "aud", "nonce", "iat", "c_hash", "role");

                options.TokenValidationParameters.NameClaimType = "preferred_username";
                options.TokenValidationParameters.RoleClaimType = "role";
            });

            services.AddControllersWithViews();

            services.AddHttpClient();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseDeveloperExceptionPage();

            app.UseStaticFiles();

            app.UseRouting();

            app.Use(async (ctx, next) =>
            {
                var schemes = ctx.RequestServices.GetRequiredService<IAuthenticationSchemeProvider>();
                var handlers = ctx.RequestServices.GetRequiredService<IAuthenticationHandlerProvider>();
                foreach (var scheme in await schemes.GetRequestHandlerSchemesAsync())
                {
                    var handler = await handlers.GetHandlerAsync(ctx, scheme.Name) as IAuthenticationRequestHandler;
                    if (handler != null && await handler.HandleRequestAsync())
                    {
                        // start same-site cookie special handling
                        string location = null;
                        if (ctx.Response.StatusCode == 302)
                        {
                            location = ctx.Response.Headers["location"];
                        }
                        else if (ctx.Request.Method == "GET" && !ctx.Request.Query["skip"].Any())
                        {
                            location = ctx.Request.Path + ctx.Request.QueryString + "&skip=1";
                        }

                        if (location != null)
                        {
                            ctx.Response.StatusCode = 200;
                            var html = $@"
                        <html><head>
                            <meta http-equiv='refresh' content='0;url={location}' />
                        </head></html>";
                            await ctx.Response.WriteAsync(html);
                        }
                        // end same-site cookie special handling

                        return;
                    }
                }

                await next();
            });
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(options =>
            {
                options.MapControllers();
                options.MapDefaultControllerRoute();
            });
        }
    }
}