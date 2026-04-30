using GeniusesProMax.DTOs;
using GeniusesProMax.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GeniusesProMax.Controllers
{
    [Route("api/Auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request)
        {
            try
            {
                var response = await _authService.RegisterAsync(request);

                return Ok(response);

            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new {message = ex.Message});
            }
        }

        [HttpPost("login")]
        public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
        {
            try
            {
                var response = await _authService.LoginAsync(request);

                return Ok(response);

            }
            catch (InvalidOperationException ex)
            {
                return Unauthorized(new {message = ex.Message});
            }
        }

    }
}
