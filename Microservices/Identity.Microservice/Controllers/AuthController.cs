// Controllers/AuthController.cs
using Identity.Microservice.DTOs;
using Identity.Microservice.Infrastructure.Identity;
using Identity.Microservice.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Identity.Microservice.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public AuthController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            try
            {
                var user = new ApplicationUser
                {
                    UserName = dto.Username,
                    Email = dto.Email
                };

                var result = await _userManager.CreateAsync(user, dto.Password);

                if (!result.Succeeded)
                {
                    return BadRequest(new
                    {
                        error = "Registration failed",
                        errors = result.Errors.Select(e => e.Description)
                    });
                }

                return Ok(new
                {
                    message = "User registered successfully",
                    user = new { user.Id, user.UserName, user.Email }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}