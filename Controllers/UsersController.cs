using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserManager.Models;
using UserManager.DTOs;
using UserManager.Services;
using UserManager.DTOs.APIResponses;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace UserManager.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly UserService _userService;
        private readonly UserManager<User> _userManager;
        public static class CustomClaimTypes
        {
            public const string UserId = "UserId";
            public const string Role = "Role";
            public const string Email = "Email";
            public const string UserName = "UserName";
        }

        public UsersController(UserService userService, UserManager<User> userManager)
        {
            _userService = userService;
            _userManager = userManager;
        }

        // Get filtered user list
        [HttpGet("List")]
       // [Authorize]

        public async Task<IActionResult> GetFilteredUserList([FromQuery] UserListFilterModel filter)
        {
         /*   
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null)
            {
                return Unauthorized(new ApiResponse<UserListModel>
                {
                    Error = new ErrorResponse
                    {
                        HttpStatusCode = 401,
                        IsError = true,
                        ErrorMessages = "Unauthorized"
                    },
                    IsError = true,
                    Result = false
                });
            }
            */
            var currentUser = new User
            {
                Id = 1,
                UserName = "admin",
                FirstName = "Admin",
                LastName = "User",
                Email = "",
                PhoneNumber = "",
                RoleId = 1,
                AgentId = 1,
                AgentPermission = false,
                Status=0

            };

            var serviceResponse = await _userService.GetFilteredUsersAsync(filter, currentUser);

            if (!serviceResponse.Success || serviceResponse.Data == null)
            {
                return BadRequest(new ApiResponse<UserListModel>
                {
                    Error = new ErrorResponse
                    {
                        HttpStatusCode = 400,
                        IsError = true,
                        ErrorMessages = string.Join(", ", serviceResponse.Messages)
                    },
                    IsError = true,
                    Result = false
                });
            }

            return Ok(new ApiResponse<UserListModel>
            {
                Result = true,
                Data = serviceResponse.Data,
                Error = new ErrorResponse
                {
                    HttpStatusCode = 200,
                    IsError = false,
                    ErrorMessages = ""
                },
                IsError = false
            });
        }

        // Create new user
        [HttpPost("")]
        [Authorize]
        public async Task<IActionResult> AddUser([FromBody] RegisterModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiResponse<User>
                {
                    Error = new ErrorResponse
                    {
                        HttpStatusCode = 400,
                        IsError = true,
                        ErrorMessages = "Invalid data"
                    },
                    IsError = true,
                    Result = false
                });
            }

            var currentUser = await GetCurrentUserAsync();

            if (currentUser == null)
            {
                return Unauthorized(new ApiResponse<User>
                {
                    Error = new ErrorResponse
                    {
                        HttpStatusCode = 401,
                        IsError = true,
                        ErrorMessages = "Unauthorized"
                    },
                    IsError = true,
                    Result = false
                });
            }

            var result = await _userService.CreateUserAsync(model, currentUser);
            var response = new ApiResponse<User>();

            if (!result.Success || result.Data == null)
            {
                response.Error = new ErrorResponse
                {
                    HttpStatusCode = 400,
                    IsError = true,
                    ErrorMessages = string.Join(", ", result.Messages)
                };
                response.IsError = true;
                response.Result = false;
                return BadRequest(response);
            }

            response.Result = true;
            response.Data = result.Data;
            response.IsError = false;
            response.Error = new ErrorResponse
            {
                HttpStatusCode = 201,
                IsError = false,
                ErrorMessages = ""
            };

            return CreatedAtAction(nameof(GetUserById), new { id = result.Data.Id }, response);
        }

        // Get current user
        [HttpGet("Me")]
        [Authorize]
        public async Task<IActionResult> GetCurrentUser()
        {
            var user = await GetCurrentUserAsync();

            if (user == null)
            {
                return Unauthorized(new ApiResponse<User>
                {
                    Error = new ErrorResponse
                    {
                        HttpStatusCode = 401,
                        IsError = true,
                        ErrorMessages = "User not found or unauthorized access."
                    },
                    IsError = true,
                    Result = false
                });
            }

            var userMasked = new User
            {
                Id = user.Id,
                UserName = user.UserName,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email ?? "",
                PhoneNumber = user.PhoneNumber ?? "",
                RoleId = user.RoleId,
                AgentId = user.AgentId,
                AgentPermission = user.AgentPermission
            };

            return Ok(new ApiResponse<User>
            {
                Data = userMasked,
                Error = new ErrorResponse
                {
                    HttpStatusCode = 200,
                    IsError = false,
                    ErrorMessages = ""
                },
                IsError = false,
                Result = true
            });
        }

        // Get user by ID
        [HttpGet("{id}")]
       // [Authorize]
        public async Task<IActionResult> GetUserById(int id)
        {
            /*
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null)
            {
                return Unauthorized(new ApiResponse<UserDetailModel>
                {
                    Error = new ErrorResponse
                    {
                        HttpStatusCode = 401,
                        IsError = true,
                        ErrorMessages = "Unauthorized"
                    },
                    IsError = true,
                    Result = false
                });
            }
            */

            var result = await _userService.GetUserDetailByIdAsync(id);

            if (!result.Success || result.Data == null)
            {
                return NotFound(new ApiResponse<UserDetailModel>
                {
                    Error = new ErrorResponse
                    {
                        HttpStatusCode = 404,
                        IsError = true,
                        ErrorMessages = string.Join(", ", result.Messages)
                    },
                    IsError = true,
                    Result = false
                });
            }

            return Ok(new ApiResponse<UserDetailModel>
            {
                Result = true,
                Data = result.Data,
                Error = new ErrorResponse
                {
                    HttpStatusCode = 200,
                    IsError = false,
                    ErrorMessages = ""
                },
                IsError = false
            });
        }


        // Update user
        [HttpPut("{id}")]
        // [Authorize]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UserEditModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiResponse<User>
                {
                    Error = new ErrorResponse
                    {
                        HttpStatusCode = 400,
                        IsError = true,
                        ErrorMessages = "Invalid data"
                    },
                    IsError = true,
                    Result = false
                });
            }
            /*
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null)
            {
                return Unauthorized(new ApiResponse<User>
                {
                    Error = new ErrorResponse
                    {
                        HttpStatusCode = 401,
                        IsError = true,
                        ErrorMessages = "Unauthorized"
                    },
                    IsError = true,
                    Result = false
                });
            }
            */
            var currentUser = new User
            {
                Id = 1,
                UserName = "admin",
                FirstName = "Admin",
                LastName = "User",
                Email = "",
                PhoneNumber = "",
                RoleId = 1,
                AgentId = 1,
                AgentPermission = false,
                Status=0

            };

            var result = await _userService.UpdateUserAsync(id, model, currentUser);
            var response = new ApiResponse<User>();

            if (result.Success && result.Data != null)
            {
                response.Result = true;
                response.Data = result.Data;
                response.Error = new ErrorResponse
                {
                    HttpStatusCode = 200,
                    IsError = false,
                    ErrorMessages = ""
                };
                response.IsError = false;
                return Ok(response);
            }
            else
            {
                response.Result = false;
                response.Error = new ErrorResponse
                {
                    HttpStatusCode = 400,
                    IsError = true,
                    ErrorMessages = string.Join(", ", result.Messages)
                };
                response.IsError = true;
                return BadRequest(response);
            }
        }

        // Delete user
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null)
            {
                return Unauthorized(new ApiResponse<bool>
                {
                    Error = new ErrorResponse
                    {
                        HttpStatusCode = 401,
                        IsError = true,
                        ErrorMessages = "Unauthorized"
                    },
                    IsError = true,
                    Result = false
                });
            }

            var result = await _userService.DeleteUserAsync(id, currentUser);
            var response = new ApiResponse<bool>();

            if (result.Success && result.Data)
            {
                response.Result = true;
                response.Data = true;
                response.Error = new ErrorResponse
                {
                    HttpStatusCode = 200,
                    IsError = false,
                    ErrorMessages = ""
                };
                response.IsError = false;
                return Ok(response);
            }
            else
            {
                response.Result = false;
                response.Data = false;
                response.Error = new ErrorResponse
                {
                    HttpStatusCode = 400,
                    IsError = true,
                    ErrorMessages = string.Join(", ", result.Messages)
                };
                response.IsError = true;
                return BadRequest(response);
            }
        }

        // Helper method to get current user
        private async Task<User?> GetCurrentUserAsync()
        {
            var userIdClaim = User.FindFirst(CustomClaimTypes.UserId);
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
            {
                return await _userManager.Users.FirstOrDefaultAsync(u => u.Id == userId);
            }
            return null;
        }
    }
}
