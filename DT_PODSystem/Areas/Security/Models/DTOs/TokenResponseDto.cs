using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DT_PODSystem.Areas.Security.Models.DTOs
{
    /// <summary>
    /// Token response from API authentication endpoint
    /// Matches the new API response format:
    /// {"token":"eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...","expires":"2025-07-20T21:14:01.4572048Z","message":"Authentication successful"}
    /// </summary>
    public class TokenResponseDto
    {
        /// <summary>
        /// JWT token string
        /// </summary>
        [Required]
        [JsonPropertyName("token")]
        public string Token { get; set; }

        /// <summary>
        /// Token expiration date and time
        /// </summary>
        [Required]
        [JsonPropertyName("expires")]
        public DateTime Expires { get; set; }

        /// <summary>
        /// Authentication response message
        /// </summary>
        [JsonPropertyName("message")]
        public string Message { get; set; }

        /// <summary>
        /// Check if token is still valid (not expired)
        /// </summary>
        public bool IsValid => DateTime.UtcNow.AddHours(3) < Expires;

        /// <summary>
        /// Time remaining until token expires
        /// </summary>
        public TimeSpan TimeToExpiry => Expires - DateTime.UtcNow.AddHours(3);

        /// <summary>
        /// Check if token will expire within specified minutes
        /// </summary>
        /// <param name="minutes">Minutes to check</param>
        /// <returns>True if token expires within the specified time</returns>
        public bool ExpiresWithin(int minutes)
        {
            return TimeToExpiry.TotalMinutes <= minutes;
        }
    }
}
