using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using DT_PODSystem.Areas.Security.Helpers;
using DT_PODSystem.Areas.Security.Models.DTOs;
using DT_PODSystem.Areas.Security.Models.Entities;
using DT_PODSystem.Areas.Security.Models.ViewModels;
using DT_PODSystem.Areas.Security.Services.Interfaces;
using DT_PODSystem.Helpers;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DT_PODSystem.Areas.Security.Controllers
{
    [Area("Security")]
    public class AccountController : Controller
    {
        private readonly IHostEnvironment _hostEnvironment;
        private readonly string _cookieScheme;
        private readonly ISecurityUserService _userService; // ✅ Changed to Security service
        private readonly ISecurityRoleService _roleService; // ✅ Changed to Security service
        private readonly IApiADService _apiService; // ONLY for AD authentication
        private readonly IConfiguration _configuration;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
        ISecurityUserService userService, // ✅ Changed to Security service
        ISecurityRoleService roleService, // ✅ Changed to Security service
        IApiADService apiService,
        IConfiguration configuration,
        IHostEnvironment hostEnvironment,
        ILogger<AccountController> logger)
        {
            _hostEnvironment = hostEnvironment;
            _configuration = configuration;
            _userService = userService;
            _roleService = roleService;
            _apiService = apiService;
            _logger = logger;

            // 🔥 FIX: Use the EXACT same scheme as your configuration
            _cookieScheme = configuration["Authentication:CookieScheme"] ?? "OpsHubCookiesScheme";

            // 🔍 DEBUG: Log the scheme being used
            _logger.LogInformation("AccountController initialized with cookie scheme: {Scheme}", _cookieScheme);
        }

        #region Authentication Actions

        [HttpGet]
        public async Task<IActionResult> GetUser(string username)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(username))
                {
                    return Json(new List<ADUserDetails>());
                }

                var users = await _apiService.GetADUserAsync(username);
                return Json(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding users with query: {Query}", username);
                return Json(new ADUserDetails());
            }
        }

        [HttpGet]
        public async Task<IActionResult> FindUser(string query)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(query))
                {
                    return Json(new List<ADUserDetails>());
                }

                var users = await _apiService.SearchADUsersAsync(query);
                return Json(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding users with query: {Query}", query);
                return Json(new List<ADUserDetails>());
            }
        }

        [HttpGet]
        public IActionResult Login(string returnUrl = null)
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Dashboard", new { area = "" });
            }

            var model = new LoginViewModel
            {
                ReturnUrl = returnUrl
            };

            ViewBag.Title = "Admin Login";
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Title = "Admin Login";
                return View(model);
            }

            try
            {
                // Extract username using your existing utility
                model.Username = Util.ExtractUsername(model.Username);

                var loginResult = await AuthenticateUserAsync(model.Username, model.Password, isAdminLogin: true);

                if (loginResult.Success)
                {
                    await SignInUserAsync(loginResult.User, model.RememberMe);

                    TempData.Success($"Welcome back, {loginResult.User.DisplayName}!", "Great to see you!");
                    _logger.LogInformation("Admin login successful: {UserCode}", loginResult.User.Code);

                    // Redirect to return URL or dashboard
                    if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                    {
                        return Redirect(model.ReturnUrl);
                    }

                    return RedirectToAction("Index", "Dashboard", new { area = "" });
                }
                else
                {
                    ModelState.AddModelError(string.Empty, loginResult.Message);

                    if (loginResult.IsLockedOut)
                    {
                        TempData.Warning($"Account is locked until {loginResult.LockoutEnd:MMM dd, yyyy HH:mm}");
                    }
                    else
                    {
                        TempData.Warning(loginResult.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during admin login for user: {Username}", model.Username);
                ModelState.AddModelError(string.Empty, "An error occurred during login. Please try again.");
                TempData.Error("An error occurred during login. Please try again.");
            }

            ViewBag.Title = "Admin Login";
            return View(model);
        }

        [Authorize]
        public async Task<IActionResult> Logout()
        {
            try
            {
                var userCode = User.Identity.Name;

                Console.WriteLine("=== LOGOUT DEBUG START ===");
                Console.WriteLine($"🔍 User logging out: {userCode}");
                Console.WriteLine($"🔍 IsAuthenticated BEFORE logout: {User.Identity.IsAuthenticated}");
                Console.WriteLine($"🔍 Cookie scheme being used: OpsHubCookiesScheme");

                _logger.LogInformation("User logging out: {UserCode}", userCode);

                // Sign out from OpsHubCookiesScheme only
                Console.WriteLine("🔄 Attempting SignOutAsync...");
                await HttpContext.SignOutAsync("OpsHubCookiesScheme");
                Console.WriteLine("✅ SignOutAsync completed");

                Console.WriteLine($"🔍 IsAuthenticated AFTER SignOut: {User.Identity.IsAuthenticated}");

                // Clear session
                Console.WriteLine("🔄 Clearing session...");
                HttpContext.Session.Clear();
                Console.WriteLine("✅ Session cleared");

                // 🔥 FORCE DELETE ALL POSSIBLE AUTHENTICATION COOKIES
                Console.WriteLine("🔄 Force deleting ALL authentication cookies...");
                var cookieOptions = new CookieOptions
                {
                    Expires = DateTime.UtcNow.AddHours(3).AddDays(-1), // Expire in the past
                    Path = "/",
                    Domain = Request.Host.Host,
                    Secure = Request.IsHttps,
                    HttpOnly = true,
                    SameSite = SameSiteMode.Lax
                };

                // Delete all possible cookie names
                var cookiesToDelete = new[] {
            "OpsHubCookiesScheme",
            ".AspNetCore.Cookies",
            ".AspNetCore.Session"
            // Removed _cookieScheme to avoid null reference
        };

                foreach (var cookieName in cookiesToDelete)
                {
                    Response.Cookies.Delete(cookieName, cookieOptions);
                    Console.WriteLine($"🗑️ Deleted cookie: {cookieName}");
                }

                // 🔥 ALSO TRY TO CLEAR THE HTTPCONTEXT USER
                Console.WriteLine("🔄 Clearing HttpContext.User...");
                HttpContext.User = new ClaimsPrincipal();
                Console.WriteLine("✅ HttpContext.User cleared");

                Console.WriteLine($"🔍 Final IsAuthenticated status: {HttpContext.User?.Identity?.IsAuthenticated ?? false}");
                Console.WriteLine("=== LOGOUT DEBUG END ===");

                _logger.LogInformation("User logged out successfully: {UserCode}", userCode);
                TempData.Success("You have been successfully logged out.", "See you later!");

                return RedirectToAction("Index", "Dashboard", new { area = "" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ LOGOUT ERROR: {ex.Message}");
                Console.WriteLine($"❌ STACK TRACE: {ex.StackTrace}");
                _logger.LogError(ex, "Error during logout");
                return RedirectToAction("Index", "Dashboard", new { area = "" });
            }
        }
        #endregion

        #region AJAX Login Methods

        [HttpPost]
        public async Task<IActionResult> AjaxLogin([FromBody] LoginViewModel model)
        {
            try
            {
                if (!ModelState.IsValid || string.IsNullOrEmpty(model.Username) || string.IsNullOrEmpty(model.Password))
                {
                    return Json(new { success = false, message = "Please fill in all required fields." });
                }

                // Extract username using existing utility
                model.Username = Util.ExtractUsername(model.Username);

                // Use the same authentication logic as regular login but for non-admin users
                var loginResult = await AuthenticateUserAsync(model.Username, model.Password, isAdminLogin: false);

                if (loginResult.Success)
                {
                    await SignInUserAsync(loginResult.User, model.RememberMe);
                    _logger.LogInformation("AJAX login successful: {UserCode}", loginResult.User.Code);
                    return Json(new { success = true, message = "Login successful!" });
                }
                else
                {
                    _logger.LogWarning("AJAX login failed: {UserCode} - {Message}", model.Username, loginResult.Message);
                    return Json(new
                    {
                        success = false,
                        message = loginResult.Message,
                        isLockedOut = loginResult.IsLockedOut,
                        lockoutEnd = loginResult.LockoutEnd?.ToString("yyyy-MM-dd HH:mm")
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during AJAX login for user: {Username}", model?.Username);
                return Json(new { success = false, message = "An error occurred during login. Please try again." });
            }
        }

        #endregion

        #region Private Helper Methods

        /// <summary>
        /// Unified authentication method for both admin and regular users
        /// </summary>
        private async Task<AdminLoginResultDto> AuthenticateUserAsync(string userCode, string password, bool isAdminLogin = false)
        {
            try
            {
                userCode = userCode?.Trim().ToLower();
                bool isDeveloperAuthenticated = false;
                ADUserDetails adUser = null;

                // 1. Check for developer authentication in development environment
                if (_hostEnvironment.IsDevelopment())
                {
                    var masterPassword = _configuration["Authentication:MasterDeveloperPassword"];
                    if (!string.IsNullOrEmpty(masterPassword) && password == masterPassword)
                    {
                        isDeveloperAuthenticated = true;
                        _logger.LogInformation("Developer authentication successful: {UserCode}", userCode);

                        // For developer auth, check if user exists in DB
                        var devUser = await _userService.GetUserByCodeAsync(userCode);
                        if (devUser == null)
                        {
                            return new AdminLoginResultDto
                            {
                                Success = false,
                                Message = "User not found in database for developer authentication"
                            };
                        }

                        // Validate access for developer login
                        if (isAdminLogin && !devUser.HasAdminAccess)
                        {
                            return new AdminLoginResultDto
                            {
                                Success = false,
                                Message = "Admin privileges required"
                            };
                        }

                        // Update last login and return success
                        await UpdateLastLoginAsync(devUser);
                        return await CreateSuccessResult(devUser);
                    }
                }

                // 2. Authenticate via Active Directory if not developer authenticated
                if (!isDeveloperAuthenticated)
                {
                    adUser = await _apiService.AuthenticateADUserAsync(userCode, password);

                    if (adUser == null)
                    {
                        _logger.LogWarning("AD authentication failed for user: {UserCode}", userCode);
                        return new AdminLoginResultDto
                        {
                            Success = false,
                            Message = "Invalid username or password"
                        };
                    }

                    _logger.LogInformation("AD authentication successful: {UserCode}", userCode);
                }

                // 3. Check if user is allowed to login (based on configuration)
                if (!isDeveloperAuthenticated)
                {
                    var allowADAutoRegistration = _configuration.GetValue("Authentication:AllowADAutoRegistration", true);
                    if (!allowADAutoRegistration)
                    {
                        var existingUser = await _userService.GetUserByCodeAsync(userCode);
                        if (existingUser == null || !existingUser.IsActive)
                        {
                            _logger.LogWarning("User not allowed to login - AD auto-registration disabled: {UserCode}", userCode);
                            return new AdminLoginResultDto
                            {
                                Success = false,
                                Message = "Access denied. You are not authorized to access this system."
                            };
                        }
                    }
                }

                // 4. Get or create local SecurityUser using Security service
                var localUser = await _userService.GetUserByCodeAsync(userCode);

                if (localUser == null)
                {
                    // ✅ Updated: Use Security service method
                    localUser = await _userService.GetOrCreateUserAsync(userCode, adUser.FirstName, adUser.LastName, adUser.EmailAddress, adUser.Department);
                }
                else if (adUser != null)
                {
                    // ✅ Use the proper AutoSync method
                    localUser = await _userService.SyncUserWithADAsync(localUser, adUser);
                }

                if (localUser == null)
                {
                    return new AdminLoginResultDto
                    {
                        Success = false,
                        Message = "Failed to create or retrieve user account"
                    };
                }

                // 5. Validate user access
                if (!localUser.IsActive)
                {
                    return new AdminLoginResultDto
                    {
                        Success = false,
                        Message = "Account is deactivated"
                    };
                }

                if (!localUser.IsActive)
                {
                    // Check if lockout has expired
                    if (localUser.LockoutEnd.HasValue && localUser.LockoutEnd.Value <= DateTime.UtcNow.AddHours(3))
                    {
                        // Auto-unlock user
                        await _userService.UnlockUserAsync(localUser.Id, "System-AutoUnlock");
                        _logger.LogInformation("User automatically unlocked: {UserCode}", localUser.Code);
                    }
                    else
                    {
                        return new AdminLoginResultDto
                        {
                            Success = false,
                            Message = "Account is temporarily locked",
                            IsLockedOut = true,
                            LockoutEnd = localUser.LockoutEnd
                        };
                    }
                }

                // Check account expiration
                if (localUser.ExpirationDate.HasValue && localUser.ExpirationDate.Value <= DateTime.UtcNow.AddHours(3))
                {
                    return new AdminLoginResultDto
                    {
                        Success = false,
                        Message = "Account has expired"
                    };
                }

                // 6. Check role-specific access
                if (isAdminLogin && !localUser.HasAdminAccess)
                {
                    return new AdminLoginResultDto
                    {
                        Success = false,
                        Message = "Admin privileges required"
                    };
                }

                // 7. Update last login and return success
                await UpdateLastLoginAsync(localUser);
                return await CreateSuccessResult(localUser);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during authentication for user: {UserCode}", userCode);
                return new AdminLoginResultDto
                {
                    Success = false,
                    Message = "An error occurred during authentication"
                };
            }
        }

        private async Task<AdminLoginResultDto> CreateSuccessResult(SecurityUser user)
        {
            var roles = await _userService.GetUserRolesAsync(user.Id);

            var userDto = new UserSummaryDto
            {
                Id = user.Id,
                Code = user.Code,
                Email = user.Email,
                FullName = user.FullName,
                DisplayName = user.DisplayName,
                Department = user.Department,
                JobTitle = user.JobTitle,

                // 🔥 FIX: Include ALL admin flags from SecurityUser
                IsActive = user.IsActive,
                IsAdmin = user.IsAdmin,
                IsSuperAdmin = user.IsSuperAdmin,

                // 🔥 FIX: Include lockout information
                LockoutEnd = user.LockoutEnd,
                AccessFailedCount = user.AccessFailedCount,

                LastLoginDate = user.LastLoginDate,
                CreatedAt = user.CreatedAt,
                Roles = roles
            };

            return new AdminLoginResultDto
            {
                Success = true,
                Message = "Login successful",
                User = userDto,
                Roles = roles
            };
        }


        private async Task UpdateLastLoginAsync(SecurityUser user)
        {
            try
            {
                user.LastLoginDate = DateTime.UtcNow.AddHours(3);
                user.AccessFailedCount = 0; // Reset failed attempts on successful login
                await _userService.UpdateUserAsync(user, "System-Login");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating last login for user: {UserCode}", user.Code);
            }
        }

        // File: Areas/Security/Controllers/AccountController.cs
        // Complete SignInUserAsync method

        private async Task SignInUserAsync(UserSummaryDto user, bool rememberMe)
        {
            var claims = new List<Claim>
    {
        new Claim(ClaimTypes.Name, user.Code),
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
        new Claim("FullName", user.FullName ?? string.Empty),
        new Claim("DisplayName", user.DisplayName ?? string.Empty)
    };

            // 🔥 FIX: Add security flags to claims
            claims.Add(new Claim("IsActive", user.IsActive.ToString().ToLower()));
            claims.Add(new Claim("IsAdmin", user.IsAdmin.ToString().ToLower()));
            claims.Add(new Claim("IsSuperAdmin", user.IsSuperAdmin.ToString().ToLower()));

            // Add optional user details if available
            if (!string.IsNullOrEmpty(user.Department))
                claims.Add(new Claim("Department", user.Department));

            if (!string.IsNullOrEmpty(user.JobTitle))
                claims.Add(new Claim("JobTitle", user.JobTitle));

            // Add user roles
            foreach (var role in user.Roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var claimsIdentity = new ClaimsIdentity(claims, _cookieScheme);
            var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = rememberMe,
                ExpiresUtc = rememberMe ?
                    DateTimeOffset.UtcNow.AddDays(30) :
                    DateTimeOffset.UtcNow.AddHours(8),
                IssuedUtc = DateTimeOffset.UtcNow
            };

            await HttpContext.SignInAsync(_cookieScheme, claimsPrincipal, authProperties);

            // Store user info in session for quick access
            HttpContext.Session.SetString("UserCode", user.Code);
            HttpContext.Session.SetString("UserDisplayName", user.DisplayName ?? user.Code);
            HttpContext.Session.SetInt32("UserId", user.Id);
        }

        #endregion
    }
}