using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using UserManager.Models;
using UserManager.DTOs;
using UserManager.Data;
using UserManager.DTOs.ServiceResponses;
using System.Text.Json;
using StackExchange.Redis;
using UserManager.Constants;
using Microsoft.Extensions.Logging;
using Role = UserManager.Models.Role;

namespace UserManager.Services
{
    public class UserService
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<Role> _roleManager;
        private readonly ApplicationDbContext _context;
        private readonly StackExchange.Redis.IConnectionMultiplexer _redis;
        private readonly ILogger<UserService> _logger;

        public UserService(
            UserManager<User> userManager,
            RoleManager<Role> roleManager,
            ApplicationDbContext context,
            IConnectionMultiplexer redis,
            ILogger<UserService> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
            _redis = redis;
            _logger = logger;
        }

        // Get all users
        public async Task<ServiceResponse<List<User>>> GetAllUsersAsync()
        {
            var response = new ServiceResponse<List<User>>
            {
                Data = await _userManager.Users.ToListAsync()
            };
            _logger.LogInformation("Fetched all users from the database.");
            return response;
        }

        // Get user details by ID
        public async Task<ServiceResponse<UserDetailModel>> GetUserDetailByIdAsync(int userId)
        {
            var response = new ServiceResponse<UserDetailModel>();
            var db = _redis.GetDatabase();
            string cacheKey = CacheKeys.UserDetail(userId);

            try
            {
                _logger.LogInformation("Attempting to retrieve user details from cache for UserID: {UserId}", userId);
                var cachedUser = await db.StringGetAsync(cacheKey);
                if (cachedUser.HasValue)
                {
                    _logger.LogInformation("User details found in cache for UserID: {UserId}", userId);
                    var userDetail = JsonSerializer.Deserialize<UserDetailModel>(cachedUser!);
                    response.Data = userDetail!;
                    return response;
                }

                _logger.LogInformation("User details not found in cache for UserID: {UserId}. Fetching from database.", userId);
                var user = await _userManager.FindByIdAsync(userId.ToString());

                if (user == null)
                {
                    _logger.LogWarning("User with ID {UserId} not found.", userId);
                    response.Success = false;
                    response.Messages.Add("User not found");
                    return response;
                }

                var role = await _roleManager.FindByIdAsync(user.RoleId.ToString());

                var address = user.AddressId.HasValue
                    ? await _context.Addresses.FirstOrDefaultAsync(a => a.Id == user.AddressId.Value)
                    : null;

                var agent = user.AgentId.HasValue
                    ? await _context.Agents.FirstOrDefaultAsync(a => a.Id == user.AgentId.Value)
                    : null;

                var companyInfo = !string.IsNullOrEmpty(user.CompanyId)
                    ? await _context.CompanyInformations.FirstOrDefaultAsync(c => c.IATA == user.CompanyId)
                    : null;

                response.Data = new UserDetailModel
                {
                    Id = user.Id,
                    Username = user.UserName!,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    RoleTitle = role?.Title ?? "",
                    RoleId = user.RoleId!,
                    Email = user.Email!,
                    Phone = user.PhoneNumber!,
                    Address = address,
                    Agent = agent,
                    CompanyInfo = companyInfo,
                    Status = user.Status
                };

                var userDetailJson = JsonSerializer.Serialize(response.Data);
                await db.StringSetAsync(cacheKey, userDetailJson, TimeSpan.FromHours(4));
                _logger.LogInformation("User details cached for UserID: {UserId} with TTL of 4 hours.", userId);
            }
            catch (RedisConnectionException ex)
            {
                _logger.LogError(ex, "Redis connection failed while fetching user details for UserID: {UserId}", userId);
                response.Success = false;
                response.Messages.Add("Unable to connect to cache. Please try again later.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching user details for UserID: {UserId}", userId);
                response.Success = false;
                response.Messages.Add("An unexpected error occurred.");
            }

            return response;
        }

        // Create new user
        public async Task<ServiceResponse<User>> CreateUserAsync(RegisterModel userDto, User currentUser)
        {
            var response = new ServiceResponse<User>();

            if (currentUser == null)
            {
                _logger.LogWarning("Unauthorized attempt to create a new user.");
                response.Success = false;
                response.Messages.Add("Unauthorized");
                return response;
            }

            var isRoleValid = await _roleManager.Roles.AnyAsync(r => r.Id == userDto.RoleId);
            if (!isRoleValid)
            {
                _logger.LogWarning("Invalid role ID {RoleId} provided for user creation.", userDto.RoleId);
                response.Success = false;
                response.Messages.Add("Invalid role");
                return response;
            }

            if (!IsAuthorized(currentUser.RoleId, userDto.RoleId))
            {
                _logger.LogWarning("User with ID {EditorId} is not authorized to assign role ID {RoleId}.", currentUser.Id, userDto.RoleId);
                response.Success = false;
                response.Messages.Add("You are not authorized to assign this role");
                return response;
            }

            var isCompanyValid = await _context.CompanyInformations.AnyAsync(c => c.IATA == userDto.CompanyId);
            if (!isCompanyValid)
            {
                _logger.LogWarning("Invalid company ID {CompanyId} provided for user creation.", userDto.CompanyId);
                response.Success = false;
                response.Messages.Add("Invalid company");
                return response;
            }

            var isAgentValid = await _context.Agents.AnyAsync(a => a.Id == userDto.AgentId);
            if (!isAgentValid)
            {
                _logger.LogWarning("Invalid agent ID {AgentId} provided for user creation.", userDto.AgentId);
                response.Success = false;
                response.Messages.Add("Invalid agent");
                return response;
            }

            var user = new User
            {
                UserName = userDto.UserName,
                FirstName = userDto.FirstName,
                LastName = userDto.LastName,
                Email = userDto.Email,
                PhoneNumber = userDto.Phone,
                AgentId = userDto.AgentId,
                RoleId = userDto.RoleId,
                CompanyId = userDto.CompanyId,
                AgentPermission = userDto.AgentPermission,
                Status = 2 // Pending approval
            };

            var result = await _userManager.CreateAsync(user, userDto.Password);
            if (result.Succeeded)
            {
                _logger.LogInformation("User with ID {UserId} created successfully.", user.Id);
                response.Data = user;
            }
            else
            {
                _logger.LogWarning("User creation failed for username {UserName}. Errors: {Errors}", userDto.UserName, string.Join(", ", result.Errors.Select(e => e.Description)));
                response.Success = false;
                response.Messages.Add("User creation failed");
                response.Messages.AddRange(result.Errors.Select(e => e.Description));
            }

            return response;
        }

        // Update user
        public async Task<ServiceResponse<User>> UpdateUserAsync(int userId, UserEditModel userDto, User currentUser)
        {
            var response = new ServiceResponse<User>();

            var user = await _userManager.FindByIdAsync(userId.ToString());

            if (user == null)
            {
                _logger.LogWarning("Attempted to update non-existent user with ID {UserId}.", userId);
                response.Success = false;
                response.Messages.Add("User not found");
                return response;
            }

            if (!HasAccess(currentUser, user))
            {
                _logger.LogWarning("User with ID {EditorId} is not authorized to update user with ID {UserId}.", currentUser.Id, userId);
                response.Success = false;
                response.Messages.Add("Unauthorized to update this user");
                return response;
            }

            if (userDto.RoleId.HasValue)
            {
                var isRoleValid = await _roleManager.Roles.AnyAsync(r => r.Id == userDto.RoleId);
                if (!isRoleValid)
                {
                    _logger.LogWarning("Invalid role ID {RoleId} provided for user update.", userDto.RoleId);
                    response.Success = false;
                    response.Messages.Add("Role not found");
                    return response;
                }

                if (!IsAuthorized(currentUser.RoleId, userDto.RoleId.Value))
                {
                    _logger.LogWarning("User with ID {EditorId} is not authorized to assign role ID {RoleId}.", currentUser.Id, userDto.RoleId.Value);
                    response.Success = false;
                    response.Messages.Add("Unauthorized to assign this role");
                    return response;
                }

                user.RoleId = userDto.RoleId.Value;
            }

            if (userDto.AgentId.HasValue)
            {
                var isAgentValid = await _context.Agents.AnyAsync(a => a.Id == userDto.AgentId);
                if (!isAgentValid)
                {
                    _logger.LogWarning("Invalid agent ID {AgentId} provided for user update.", userDto.AgentId);
                    response.Success = false;
                    response.Messages.Add("Agent not found");
                    return response;
                }
                user.AgentId = userDto.AgentId.Value;
            }

            if (!string.IsNullOrEmpty(userDto.CompanyId))
            {
                var isCompanyValid = await _context.CompanyInformations.AnyAsync(c => c.IATA == userDto.CompanyId);
                if (!isCompanyValid)
                {
                    _logger.LogWarning("Invalid company ID {CompanyId} provided for user update.", userDto.CompanyId);
                    response.Success = false;
                    response.Messages.Add("Company not found");
                    return response;
                }
                user.CompanyId = userDto.CompanyId;
            }

            if (userDto.AgentPermission.HasValue)
                user.AgentPermission = userDto.AgentPermission.Value;

            if (userDto.Status.HasValue)
            {
                if (userDto.Status.Value == 0 || userDto.Status.Value == 1 || userDto.Status.Value == 2)
                    user.Status = userDto.Status.Value;
                else
                {
                    _logger.LogWarning("Invalid status value {Status} provided for user update.", userDto.Status.Value);
                    response.Success = false;
                    response.Messages.Add("Invalid status");
                    return response;
                }
            }

            if (!string.IsNullOrEmpty(userDto.FirstName))
                user.FirstName = userDto.FirstName;

            if (!string.IsNullOrEmpty(userDto.LastName))
                user.LastName = userDto.LastName;

            if (!string.IsNullOrEmpty(userDto.Email))
                user.Email = userDto.Email;

            if (!string.IsNullOrEmpty(userDto.Phone))
                user.PhoneNumber = userDto.Phone;

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                _logger.LogInformation("User with ID {UserId} updated successfully.", userId);
                response.Data = user;
                // Update cache
                var db = _redis.GetDatabase();
                await db.StringSetAsync(CacheKeys.UserDetail(userId), JsonSerializer.Serialize(response.Data), TimeSpan.FromHours(4));
            }
            else
            {
                _logger.LogWarning("User update failed for user ID {UserId}. Errors: {Errors}", userId, string.Join(", ", result.Errors.Select(e => e.Description)));
                response.Success = false;
                response.Messages.Add("Update failed");
                response.Messages.AddRange(result.Errors.Select(e => e.Description));
            }

            return response;
        }

        // Delete user
        public async Task<ServiceResponse<bool>> DeleteUserAsync(int userId, User currentUser)
        {
            var response = new ServiceResponse<bool>();

            var user = await _userManager.FindByIdAsync(userId.ToString());

            if (user == null)
            {
                _logger.LogWarning("Attempted to delete non-existent user with ID {UserId}.", userId);
                response.Success = false;
                response.Messages.Add("User not found");
                response.Data = false;
                return response;
            }

            if (!HasAccess(currentUser, user))
            {
                _logger.LogWarning("User with ID {EditorId} is not authorized to delete user with ID {UserId}.", currentUser.Id, userId);
                response.Success = false;
                response.Messages.Add("Unauthorized to delete this user");
                response.Data = false;
                return response;
            }

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                _logger.LogInformation("User with ID {UserId} deleted successfully.", userId);
                response.Data = true;
                // Delete from cache
                var db = _redis.GetDatabase();
                await db.KeyDeleteAsync(CacheKeys.UserDetail(userId));
            }
            else
            {
                _logger.LogWarning("User deletion failed for user ID {UserId}. Errors: {Errors}", userId, string.Join(", ", result.Errors.Select(e => e.Description)));
                response.Success = false;
                response.Messages.Add("Deletion failed");
                response.Data = false;
            }

            return response;
        }

        // Get filtered user list
        public async Task<ServiceResponse<UserListModel>> GetFilteredUsersAsync(UserListFilterModel filter, User currentUser)
        {
            var response = new ServiceResponse<UserListModel>();

            var query = _userManager.Users.AsQueryable();

            // Apply filters
            if (!string.IsNullOrWhiteSpace(filter.Name))
            {
                query = query.Where(u => u.FirstName.ToLower().Contains(filter.Name.ToLower()));
            }

            if (!string.IsNullOrWhiteSpace(filter.Surname))
            {
                query = query.Where(u => u.LastName.ToLower().Contains(filter.Surname.ToLower()));
            }

            if (filter.RoleId.HasValue)
            {
                query = query.Where(u => u.RoleId == filter.RoleId.Value);
            }

            if (filter.Status.HasValue)
            {
                query = query.Where(u => u.Status == filter.Status.Value);
            }

            // Total user count
            var totalUsers = await query.CountAsync();

            // Pagination
            int localPageIndex = filter.PageIndex - 1 < 0 ? 0 : filter.PageIndex - 1;
            int localPageItemCount = filter.PageItemCount < 1 ? 15 : filter.PageItemCount;

            var users = await query.Skip(localPageIndex * localPageItemCount)
                                   .Take(localPageItemCount)
                                   .ToListAsync();

            // Map to UserListItem
            var userListItems = users.Select(u =>
            {
                bool accessResult = HasAccess(currentUser, u);
                string roleTitle = _roleManager.Roles.FirstOrDefault(r => r.Id == u.RoleId)?.Title ?? "Role not found";

                return new UserListItem
                {
                    Id = u.Id,
                    Username = u.UserName!,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    RoleTitle = roleTitle,
                    Email = u.Email!,
                    Phone = u.PhoneNumber!,
                    Status = u.Status,
                    CanView = accessResult,
                    CanDelete = accessResult,
                    CanEdit = accessResult,
                    CanApprove = accessResult
                };
            }).ToList();

            response.Data = new UserListModel
            {
                UserInfos = userListItems,
                TotalItemCount = totalUsers,
                PageIndex = localPageIndex + 1,
                PageItemCount = localPageItemCount
            };

            _logger.LogInformation("Fetched filtered user list. Total Users: {TotalUsers}", totalUsers);
            return response;
        }

        // Authorization helpers
        private static bool IsAuthorized(int editorRoleId, int targetRoleId)
        {
            // Simplified authorization logic
            while (editorRoleId >= 10 && targetRoleId >= 10)
            {
                editorRoleId /= 10;
                targetRoleId /= 10;
            }

            return editorRoleId < targetRoleId;
        }

        private static bool HasAccess(User editor, User target)
        {
            if (editor.Id == target.Id)
            {
                return true;
            }

            bool isAuth = IsAuthorized(editor.RoleId, target.RoleId);

            if (editor.AgentId == target.AgentId)
            {
                return isAuth && editor.Status == 0;
            }
            return false;
        }
    }
}
