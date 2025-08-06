
using System;
using System.Net;
using DT_PODSystem.Areas.Security.Helpers;
using DT_PODSystem.Areas.Security.Models.Configuration;
using DT_PODSystem.Areas.Security.Models.ViewModels;
using DT_PODSystem.Areas.Security.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DT_PODSystem.Areas.Security.Controllers
{
    [Area("Security")]
    [AllowAnonymous]
    public class ErrorController : Controller
    {
        private readonly ILogger<ErrorController> _logger;
        private readonly IHostEnvironment _environment;
        private readonly ErrorConfiguration _errorConfig;
        private readonly ISecurityAuditService _auditService;

        public ErrorController(
            ILogger<ErrorController> logger,
            IHostEnvironment environment,
            IConfiguration configuration,
            ISecurityAuditService auditService = null)
        {
            _logger = logger;
            _environment = environment;
            _auditService = auditService;

            // Load error configuration from appsettings
            _errorConfig = new ErrorConfiguration();
            configuration.GetSection("ErrorPages").Bind(_errorConfig);
        }

        #region Status Code Error Pages

        [Route("/Error/{statusCode:int}")]
        [Route("/Security/Error/{statusCode:int}")]
        public IActionResult HandleStatusCode(int statusCode)
        {
            var statusCodeResult = HttpContext.Features.Get<IStatusCodeReExecuteFeature>();
            var originalPath = statusCodeResult?.OriginalPath ?? Request.Path.Value;
            var originalQueryString = statusCodeResult?.OriginalQueryString ?? Request.QueryString.Value;

            // Log the error
            LogStatusCodeError(statusCode, originalPath, originalQueryString);

            // Create error model
            var errorModel = CreateErrorModel(statusCode, originalPath);

            // Check if it's an AJAX request
            if (IsAjaxRequest())
            {
                return Json(new
                {
                    success = false,
                    statusCode = statusCode,
                    message = errorModel.Message,
                    redirectUrl = GetRedirectUrl(statusCode)
                });
            }

            // Return appropriate view
            return View("ErrorPage", errorModel);
        }

        // Specific error action routes for backward compatibility
        [Route("/Error/404")]
        [Route("/Security/Error/404")]
        public IActionResult NotFound() => HandleStatusCode(404);

        [Route("/Error/403")]
        [Route("/Security/Error/403")]
        public IActionResult Forbidden() => HandleStatusCode(403);

        [Route("/Error/401")]
        [Route("/Security/Error/401")]
        public IActionResult Unauthorized() => HandleStatusCode(401);

        [Route("/Error/500")]
        [Route("/Security/Error/500")]
        public IActionResult InternalServerError() => HandleStatusCode(500);

        [Route("/Error/408")]
        [Route("/Security/Error/408")]
        public IActionResult RequestTimeout() => HandleStatusCode(408);

        [Route("/Error/429")]
        [Route("/Security/Error/429")]
        public IActionResult TooManyRequests() => HandleStatusCode(429);

        #endregion

        #region Exception Error Page

        [Route("/Error")]
        [Route("/Security/Error")]
        public IActionResult Error()
        {
            var exceptionFeature = HttpContext.Features.Get<IExceptionHandlerFeature>();
            var exception = exceptionFeature?.Error;

            if (exception != null)
            {
                // Log the exception
                LogException(exception);

                // Determine status code from exception
                var statusCode = GetStatusCodeFromException(exception);

                // Create error model
                var errorModel = CreateErrorModel(statusCode, Request.Path, exception);

                // Check if it's an AJAX request
                if (IsAjaxRequest())
                {
                    return Json(new
                    {
                        success = false,
                        statusCode = statusCode,
                        message = errorModel.Message,
                        error = _environment.IsDevelopment() ? exception.Message : null
                    });
                }

                return View("ErrorPage", errorModel);
            }

            // Default to 500 error if no exception found
            return HandleStatusCode(500);
        }

        #endregion

        #region Access Denied (moved from Account)

        [Route("/Error/AccessDenied")]
        [Route("/Security/Error/AccessDenied")]
        public IActionResult AccessDenied(string returnUrl = null)
        {
            // Log the access denied attempt
            LogAccessDenied(returnUrl);

            var errorModel = CreateAccessDeniedModel(returnUrl);

            // Check if it's an AJAX request
            if (IsAjaxRequest())
            {
                return Json(new
                {
                    success = false,
                    statusCode = 403,
                    message = "Access denied",
                    requiresLogin = !User.Identity.IsAuthenticated,
                    redirectUrl = _errorConfig.DefaultRedirects.LoginPage
                });
            }

            return View("AccessDenied", errorModel);
        }

        #endregion

        #region Helper Methods

        private ErrorPageViewModel CreateErrorModel(int statusCode, string originalPath, Exception exception = null)
        {
            var statusCodeStr = statusCode.ToString();
            var hasCustomMessage = _errorConfig.Messages.ContainsKey(statusCodeStr);
            var customMessage = hasCustomMessage ? _errorConfig.Messages[statusCodeStr] : new ErrorMessage();

            return new ErrorPageViewModel
            {
                StatusCode = statusCode,
                Title = hasCustomMessage ? customMessage.Title : GetDefaultTitle(statusCode),
                Message = hasCustomMessage ? customMessage.Message : GetDefaultMessage(statusCode),
                Description = hasCustomMessage ? customMessage.Description : GetDefaultDescription(statusCode),
                Icon = hasCustomMessage ? customMessage.Icon : GetDefaultIcon(statusCode),
                OriginalPath = originalPath,
                ShowDetails = _environment.IsDevelopment() && _errorConfig.EnableDetailedErrors,
                Exception = exception,
                IsAuthenticated = User.Identity.IsAuthenticated,
                UserCode = User.Identity.IsAuthenticated ? User.GetUserCode() : null,
                UserDisplayName = User.Identity.IsAuthenticated ? User.GetUserDisplayName() : null,
                HomePage = _errorConfig.DefaultRedirects.HomePage,
                LoginPage = _errorConfig.DefaultRedirects.LoginPage,
                DashboardPage = _errorConfig.DefaultRedirects.DashboardPage,
                SupportEmail = _errorConfig.DefaultRedirects.SupportEmail,
                SupportPhone = _errorConfig.DefaultRedirects.SupportPhone,
                RedirectAfterSeconds = _errorConfig.RedirectAfterSeconds
            };
        }

        private ErrorPageViewModel CreateAccessDeniedModel(string returnUrl)
        {
            var model = CreateErrorModel(403, returnUrl ?? Request.Path);
            model.ReturnUrl = returnUrl;
            model.RequiresAdmin = DetermineIfRequiresAdmin(returnUrl);
            model.RequiresSuperAdmin = DetermineIfRequiresSuperAdmin(returnUrl);
            return model;
        }

        private void LogStatusCodeError(int statusCode, string originalPath, string queryString)
        {
            if (!_errorConfig.LogErrors) return;

            var userCode = User.Identity.IsAuthenticated ? User.GetUserCode() : "Anonymous";
            var fullPath = originalPath + queryString;

            _logger.LogWarning("Status code {StatusCode} for user {UserCode} accessing {Path}",
                statusCode, userCode, fullPath);

            // Log to security audit if available
            _auditService?.LogAsync($"Error_{statusCode}", "Http", statusCode.ToString(),
                User.GetUserId().ToString(), userCode,
                $"HTTP {statusCode} error for path: {fullPath}",
                this.GetClientIpAddress(), this.GetUserAgent(), false, $"HTTP {statusCode}");
        }

        private void LogException(Exception exception)
        {
            if (!_errorConfig.LogErrors) return;

            var userCode = User.Identity.IsAuthenticated ? User.GetUserCode() : "Anonymous";

            _logger.LogError(exception, "Unhandled exception for user {UserCode}", userCode);

            // Log to security audit if available
            _auditService?.LogAsync("Exception", "Application", "0",
                User.GetUserId().ToString(), userCode,
                $"Unhandled exception: {exception.Message}",
                this.GetClientIpAddress(), this.GetUserAgent(), false, exception.GetType().Name);
        }

        private void LogAccessDenied(string returnUrl)
        {
            if (!_errorConfig.LogErrors) return;

            var userCode = User.Identity.IsAuthenticated ? User.GetUserCode() : "Anonymous";
            var userRoles = User.Identity.IsAuthenticated ? string.Join(", ", User.GetUserRoles()) : "None";

            _logger.LogWarning("Access denied for user {UserCode} with roles [{Roles}] attempting to access {ReturnUrl}",
                userCode, userRoles, returnUrl ?? "Unknown");

            // Log to security audit if available
            _auditService?.LogAsync("AccessDenied", "Authorization", "0",
                User.GetUserId().ToString(), userCode,
                $"Access denied attempt to: {returnUrl ?? Request.Path}",
                this.GetClientIpAddress(), this.GetUserAgent(), false, "Insufficient privileges");
        }

        private bool IsAjaxRequest()
        {
            return Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                   Request.Headers["Accept"].ToString().Contains("application/json");
        }

        private string GetRedirectUrl(int statusCode)
        {
            return statusCode switch
            {
                401 => _errorConfig.DefaultRedirects.LoginPage,
                403 => _errorConfig.DefaultRedirects.HomePage,
                404 => _errorConfig.DefaultRedirects.HomePage,
                _ => _errorConfig.DefaultRedirects.HomePage
            };
        }

        private int GetStatusCodeFromException(Exception exception)
        {
            return exception switch
            {
                UnauthorizedAccessException => 401,
                ArgumentNullException => 400,        // More specific exception first
                ArgumentException => 400,            // More general exception second
                NotImplementedException => 501,
                TimeoutException => 408,
                _ => 500
            };
        }


        private bool DetermineIfRequiresAdmin(string returnUrl)
        {
            if (string.IsNullOrEmpty(returnUrl)) return false;
            return returnUrl.Contains("Admin", StringComparison.OrdinalIgnoreCase);
        }

        private bool DetermineIfRequiresSuperAdmin(string returnUrl)
        {
            if (string.IsNullOrEmpty(returnUrl)) return false;
            return returnUrl.Contains("SuperAdmin", StringComparison.OrdinalIgnoreCase);
        }

        // Default fallback values if not configured in appsettings
        private string GetDefaultTitle(int statusCode) => statusCode switch
        {
            404 => "Page Not Found",
            403 => "Access Denied",
            401 => "Unauthorized",
            500 => "Server Error",
            408 => "Request Timeout",
            429 => "Too Many Requests",
            _ => "Error"
        };

        private string GetDefaultMessage(int statusCode) => statusCode switch
        {
            404 => "We couldn't find what you're looking for",
            403 => "You don't have permission",
            401 => "Authentication required",
            500 => "Something went wrong",
            408 => "Request took too long",
            429 => "Too many requests",
            _ => "An error occurred"
        };

        private string GetDefaultDescription(int statusCode) => statusCode switch
        {
            404 => "The page you're looking for doesn't exist or has been moved.",
            403 => "You don't have the necessary permissions to access this resource.",
            401 => "You need to sign in to access this resource.",
            500 => "An unexpected error occurred on our server. We're working to fix it.",
            408 => "The request took longer than expected to process.",
            429 => "You've made too many requests. Please wait a moment and try again.",
            _ => "An unexpected error occurred."
        };

        private string GetDefaultIcon(int statusCode) => statusCode switch
        {
            404 => "fas fa-search",
            403 => "fas fa-shield-exclamation",
            401 => "fas fa-sign-in-alt",
            500 => "fas fa-server",
            408 => "fas fa-clock",
            429 => "fas fa-hand-paper",
            _ => "fas fa-exclamation-triangle"
        };

        #endregion
    }
}