using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UserManager.Data;
using UserManager.Models;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using UserManager.DTOs;
using UserManager.Services;
using UserManager.DTOs.APIResponses;

namespace UserManager.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly TokenService _tokenService;

        public AuthController(UserManager<User> userManager, TokenService tokenService)
        {
            _userManager = userManager;
            _tokenService = tokenService;
        }

        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            var user = await _userManager.FindByNameAsync(model.Username);  
            if (user != null && await _userManager.CheckPasswordAsync(user, model.Password))
            {
                var token = _tokenService.GenerateToken(user);
                return Ok(new { Token = token });
            }
            return Unauthorized();  
        }
    }
}
