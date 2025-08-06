using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Mail;
using System.Threading.Tasks;
using DT_PODSystem.Areas.Security.Models.DTOs;
using DT_PODSystem.Areas.Security.Models.ViewModels;
using DT_PODSystem.Areas.Security.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;


namespace DT_PODSystem.Areas.Security.Services.Implementations

{
    public class ApiADService : IApiADService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly string _baseUrl;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ApiADService(HttpClient httpClient, IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _baseUrl = configuration["ApiSettings:BaseUrl"];
            _httpContextAccessor = httpContextAccessor;
        }


        #region Authentication


        public async Task<string> GetTokenAsync()
        {
            // Read username and password from configuration
            string username = _configuration["ApiSettings:Username"];
            string password = _configuration["ApiSettings:Password"];

            var loginDto = new LoginDto
            {
                Username = username,
                Password = password
            };

            // Make the request to get the token
            var response = await _httpClient.PostAsJsonAsync($"{_configuration["ApiSettings:BaseUrl"]}/auth/get-token", loginDto);

            if (response.IsSuccessStatusCode)
            {
                // 🆕 NEW: Parse the JSON response to extract the token
                var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponseDto>();

                if (tokenResponse != null && !string.IsNullOrEmpty(tokenResponse.Token))
                {
                    return tokenResponse.Token; // Return just the token string
                }

                throw new Exception("Invalid token response format from AD API");
            }

            throw new Exception("Failed to retrieve AD token: " + response.ReasonPhrase);
        }


        private async Task<string> EnsureTokenAsync()
        {
            var token = _httpContextAccessor.HttpContext.Session.GetString("AuthToken");

            if (string.IsNullOrEmpty(token))
            {
                token = await GetTokenAsync(); // Fetch a new token if not found in session
                _httpContextAccessor.HttpContext.Session.SetString("AuthToken", token); // Store token in session for future use
            }

            return token;
        }

        #endregion


        #region AD APIs

        // NEW - Get specific user details


        public async Task<ADUserDetails> GetADUserAsync(string username)
        {
            var token = await EnsureTokenAsync();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.GetAsync($"{_baseUrl}/ad/users/get-ad-user?username={Uri.EscapeDataString(username)}");

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ADUserDetails>();
            }
            else
                return null;
        }

        public async Task<List<ADUserDetails>> SearchADUsersAsync(string searchKey)
        {
            var token = await EnsureTokenAsync();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.GetAsync($"{_baseUrl}/ad/users/search-ad?searchKey={Uri.EscapeDataString(searchKey)}");

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<List<ADUserDetails>>();
            }
            else
                return null;
        }

        public async Task<ADUserDetails> AuthenticateADUserAsync(string username, string password)
        {


            var token = await EnsureTokenAsync();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var request = new LoginDto
            {
                Username = username,
                Password = password
            };
            var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/ad/users/authenticate-ad", request);


            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ADUserDetails>();

            }
            else
                return null;

        }




        #endregion

    }
}
