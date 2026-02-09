using Identity.Microservice.DTOs;
using Identity.Microservice.Infrastructure.Identity;
using Identity.Microservice.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace Identity.Microservice.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public AuthService(
            UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<ApplicationUser> RegisterAsync(RegisterDto dto)
        {
            var user = new ApplicationUser
            {
                UserName = dto.Username,
                Email = dto.Email
            };

            var result = await _userManager.CreateAsync(user, dto.Password);

            if (!result.Succeeded)
                throw new Exception(string.Join(", ", result.Errors.Select(e => e.Description)));

            return user;
        }
    }
}