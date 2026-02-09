using Identity.Microservice.DTOs;
using Identity.Microservice.Infrastructure.Identity;

namespace Identity.Microservice.Interfaces
{
    public interface IAuthService
    {
        Task<ApplicationUser> RegisterAsync(RegisterDto dto);
    }
}
