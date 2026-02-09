using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using System.Security.Claims;
using Identity.Microservice.Infrastructure.Identity;
using Microsoft.AspNetCore;

namespace Identity.Microservice.Controllers
{
    [Route("connect")]
    [ApiController]
    public class ConnectController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public ConnectController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [HttpPost("token")]
        [IgnoreAntiforgeryToken]
        [Consumes("application/x-www-form-urlencoded")]
        [Produces("application/json")]
        public async Task<IActionResult> Token()
        {
            var request = HttpContext.GetOpenIddictServerRequest() ??
                throw new InvalidOperationException("The OpenIddict request cannot be retrieved.");

            if (request.IsPasswordGrantType())
            {
                return await HandlePasswordGrantAsync(request);
            }
            else if (request.IsRefreshTokenGrantType())
            {
                return await HandleRefreshTokenGrantAsync(request);
            }

            return BadRequest(new OpenIddictResponse
            {
                Error = OpenIddictConstants.Errors.UnsupportedGrantType,
                ErrorDescription = "The specified grant type is not supported."
            });
        }

        private async Task<IActionResult> HandlePasswordGrantAsync(OpenIddictRequest request)
        {
            var user = await _userManager.FindByNameAsync(request.Username);
            if (user == null)
            {
                return Forbid(
                    authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                    properties: new AuthenticationProperties(new Dictionary<string, string>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = OpenIddictConstants.Errors.InvalidGrant,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "Invalid username or password."
                    }));
            }

            // Validate password
            var result = await _signInManager.CheckPasswordSignInAsync(
                user, request.Password, lockoutOnFailure: true);

            if (!result.Succeeded)
            {
                return Forbid(
                    authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                    properties: new AuthenticationProperties(new Dictionary<string, string>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = OpenIddictConstants.Errors.InvalidGrant,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "Invalid username or password."
                    }));
            }

            // Create claims principal
            var principal = await CreateUserPrincipalAsync(user);

            // Sign in - OpenIddict će automatski generisati JWT token
            return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        private async Task<IActionResult> HandleRefreshTokenGrantAsync(OpenIddictRequest request)
        {
            // Retrieve the claims principal stored in the refresh token
            var result = await HttpContext.AuthenticateAsync(
                OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

            if (!result.Succeeded)
            {
                return Forbid(
                    authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                    properties: new AuthenticationProperties(new Dictionary<string, string>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = OpenIddictConstants.Errors.InvalidGrant,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The refresh token is no longer valid."
                    }));
            }

            // Retrieve the user profile
            var user = await _userManager.GetUserAsync(result.Principal);
            if (user == null)
            {
                return Forbid(
                    authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                    properties: new AuthenticationProperties(new Dictionary<string, string>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = OpenIddictConstants.Errors.InvalidGrant,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The refresh token is no longer valid."
                    }));
            }

            // Ensure the user is still allowed to sign in
            if (!await _signInManager.CanSignInAsync(user))
            {
                return Forbid(
                    authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                    properties: new AuthenticationProperties(new Dictionary<string, string>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = OpenIddictConstants.Errors.InvalidGrant,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The user is no longer allowed to sign in."
                    }));
            }

            // Create new claims principal
            var principal = await CreateUserPrincipalAsync(user);

            return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        private async Task<ClaimsPrincipal> CreateUserPrincipalAsync(ApplicationUser user)
        {
            // Create a new ClaimsIdentity with OpenIddict authentication type
            var identity = new ClaimsIdentity(
                OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                OpenIddictConstants.Claims.Name,
                OpenIddictConstants.Claims.Role);

            // Add the mandatory subject claim (user ID)
            identity.AddClaim(new Claim(OpenIddictConstants.Claims.Subject,
                await _userManager.GetUserIdAsync(user))
                .SetDestinations(OpenIddictConstants.Destinations.AccessToken,
                    OpenIddictConstants.Destinations.IdentityToken));

            // Add username claim
            identity.AddClaim(new Claim(OpenIddictConstants.Claims.Name,
                await _userManager.GetUserNameAsync(user))
                .SetDestinations(OpenIddictConstants.Destinations.AccessToken,
                    OpenIddictConstants.Destinations.IdentityToken));

            // Add email claims if available
            var email = await _userManager.GetEmailAsync(user);
            if (!string.IsNullOrEmpty(email))
            {
                identity.AddClaim(new Claim(OpenIddictConstants.Claims.Email, email)
                    .SetDestinations(OpenIddictConstants.Destinations.AccessToken,
                        OpenIddictConstants.Destinations.IdentityToken));

                identity.AddClaim(new Claim(OpenIddictConstants.Claims.EmailVerified,
                    (await _userManager.IsEmailConfirmedAsync(user)).ToString().ToLower())
                    .SetDestinations(OpenIddictConstants.Destinations.AccessToken,
                        OpenIddictConstants.Destinations.IdentityToken));
            }

            // Add role claims
            var roles = await _userManager.GetRolesAsync(user);
            foreach (var role in roles)
            {
                identity.AddClaim(new Claim(OpenIddictConstants.Claims.Role, role)
                    .SetDestinations(OpenIddictConstants.Destinations.AccessToken,
                        OpenIddictConstants.Destinations.IdentityToken));
            }

            // Create principal
            var principal = new ClaimsPrincipal(identity);

            // Set scopes
            principal.SetScopes(new[]
            {
                OpenIddictConstants.Scopes.OpenId,
                OpenIddictConstants.Scopes.Email,
                OpenIddictConstants.Scopes.Profile,
                OpenIddictConstants.Scopes.Roles,
                OpenIddictConstants.Scopes.OfflineAccess // Za refresh token
            });

            return principal;
        }

    }
}