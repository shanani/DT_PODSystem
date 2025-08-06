using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Design;
using System.Drawing.Imaging;
using System.Drawing.Internal;
using System.IO;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using DT_PODSystem.Areas.Security.Helpers;
using DT_PODSystem.Areas.Security.Models.DTOs;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DT_PODSystem.Helpers
{
    public static class Util
    {
        private static IHttpContextAccessor _httpContextAccessor;
        private static IConfiguration _configuration;
        private static string _encryptionKey = "";

        private static IServiceProvider _serviceProvider;
        public static void Configure(IHttpContextAccessor httpContextAccessor, IConfiguration configuration)
        {
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
            _encryptionKey = _configuration["Authentication:EncryptionKey"];
        }


        // Add this initialization method - call it from Program.cs
        public static void Initialize(IHttpContextAccessor httpContextAccessor, IServiceProvider serviceProvider)
        {
            _httpContextAccessor = httpContextAccessor;
            _serviceProvider = serviceProvider;
        }

        // Add this method to your existing Util.cs class in DT_PODSystem/Helpers/Util.cs

        /// <summary>
        /// Converts an image to base64 data URI format
        /// </summary>
        /// <param name="imagePath">Local file path or URL to the image</param>
        /// <returns>Base64 data URI string ready for img src attribute</returns>
        /// 
        public static string GetImage(string imagePath)
        {

            return imagePath.TrimStart('~');
        }
        //public static string GetRoot()
        //{

        //    // Get IWebHostEnvironment from DI container
        //    var environment = _httpContextAccessor.HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>();
        //    return environment.WebRootPath;
        //}
        public static string GetImageBase64(string imagePath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(imagePath))
                    return GetPlaceholderImage();

                byte[] imageBytes = null;
                string contentType = "image/jpeg";

                // Handle URL
                if (imagePath.StartsWith("http://") || imagePath.StartsWith("https://"))
                {
                    using (var httpClient = new HttpClient())
                    {
                        httpClient.Timeout = TimeSpan.FromSeconds(5);
                        var response = httpClient.GetAsync(imagePath).Result;
                        if (response.IsSuccessStatusCode)
                        {
                            imageBytes = response.Content.ReadAsByteArrayAsync().Result;
                            contentType = GetContentType(Path.GetExtension(imagePath));
                        }
                    }
                }
                // Handle local file path
                else
                {
                    string fullPath = imagePath;

                    // Convert relative paths to absolute
                    if (imagePath.StartsWith("~/"))
                    {
                        var context = _httpContextAccessor?.HttpContext;
                        if (context != null)
                        {
                            var webRoot = context.RequestServices.GetRequiredService<IWebHostEnvironment>().WebRootPath;
                            fullPath = Path.Combine(webRoot, imagePath.Substring(2));
                        }
                    }
                    else if (imagePath.StartsWith("/"))
                    {
                        var context = _httpContextAccessor?.HttpContext;
                        if (context != null)
                        {
                            var webRoot = context.RequestServices.GetRequiredService<IWebHostEnvironment>().WebRootPath;
                            fullPath = Path.Combine(webRoot, imagePath.Substring(1));
                        }
                    }

                    if (File.Exists(fullPath))
                    {
                        imageBytes = File.ReadAllBytes(fullPath);
                        contentType = GetContentType(Path.GetExtension(fullPath));
                    }
                }

                if (imageBytes != null && imageBytes.Length > 0)
                {
                    string base64String = Convert.ToBase64String(imageBytes);
                    return $"data:{contentType};base64,{base64String}";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading image: {ex.Message}");
            }

            return imagePath;
        }

        /// <summary>
        /// Gets MIME type from file extension
        /// </summary>
        private static string GetContentType(string extension)
        {
            return extension?.ToLowerInvariant() switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".webp" => "image/webp",
                ".bmp" => "image/bmp",
                ".svg" => "image/svg+xml",
                _ => "image/jpeg"
            };
        }

        /// <summary>
        /// Returns a simple placeholder image as base64
        /// </summary>
        private static string GetPlaceholderImage()
        {
            string svg = @"<svg width='200' height='200' xmlns='http://www.w3.org/2000/svg'>
        <rect width='200' height='200' fill='#f8f9fa' stroke='#dee2e6'/>
        <text x='100' y='100' text-anchor='middle' dy='0.3em' font-family='Arial' font-size='14' fill='#6c757d'>No Image</text>
    </svg>";

            return $"data:image/svg+xml;base64,{Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(svg))}";
        }


        /// <summary>
        /// Gets the current user from session/context (set by middleware)
        /// </summary>
        public static UserDto GetCurrentUser()
        {
            Console.WriteLine("🔍 [UTIL] GetCurrentUser called");

            var context = _httpContextAccessor?.HttpContext;
            if (context == null)
            {
                Console.WriteLine("❌ [UTIL] HttpContext is null");
                return new UserDto();
            }

            // Try request context first (set by middleware)
            var user = context.Items["CurrentUser"] as UserDto;
            if (user != null)
            {
                Console.WriteLine($"✅ [UTIL] Found user in context: {user.Code}, IsActive: {user.IsActive}, IsAdmin: {user.IsAdmin}");
                return user;
            }

            // Fallback to session
            user = GetObjectFromJson<UserDto>(context.Session, "User") ?? new UserDto();
            if (!string.IsNullOrEmpty(user.Code))
            {
                Console.WriteLine($"✅ [UTIL] Found user in session: {user.Code}, IsActive: {user.IsActive}, IsAdmin: {user.IsAdmin}");
            }
            else
            {
                Console.WriteLine("⚠️ [UTIL] No user found in context or session, returning empty UserDto");
            }

            return user;
        }

        #region Session Extension Methods (Moved from SessionHelper)

        private static void SetObjectAsJson(ISession session, string key, object value)
        {
            if (value == null)
            {
                session.Remove(key);
                return;
            }

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };

            session.SetString(key, JsonSerializer.Serialize(value, options));
        }

        private static T GetObjectFromJson<T>(ISession session, string key)
        {
            var value = session.GetString(key);
            if (string.IsNullOrEmpty(value))
                return default;

            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    PropertyNameCaseInsensitive = true
                };

                return JsonSerializer.Deserialize<T>(value, options);
            }
            catch
            {
                // If deserialization fails, remove the corrupted session data
                session.Remove(key);
                return default;
            }
        }

        #endregion
        public static string ExtractUsername(string email)
        {
            // Check if the email ends with "@stc.com.sa"
            string domain = "@stc.com.sa";
            if (email.EndsWith(domain))
            {
                // Remove the domain part first
                string localPart = email.Substring(0, email.Length - domain.Length);

                // Check if the local part ends with ".y" (single character after dot)
                if (localPart.Length > 2 && localPart[localPart.Length - 2] == '.' && localPart[localPart.Length - 1].ToString().Length == 1)
                {
                    // Remove the last two characters (".y")
                    localPart = localPart.Substring(0, localPart.Length - 2);
                }

                // Return the local part in lowercase and append the domain
                return localPart.ToLower();
            }

            // Return the email as is if it doesn't match the expected domain
            return email.ToLower();
        }

        public static string GenerateAvatarText(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            string[] _sFullName = text.Split(' ');
            List<string> _sResult = new List<string>();
            string _result = "";


            for (int i = 0; i <= _sFullName.Length - 1; i++)
            {
                if (_sFullName[i].Length >= 3)
                {
                    _sResult.Add(_sFullName[i]);
                }
            }

            if (_sResult.Count == 1)
            {
                _result = _sResult[0].Substring(0, 1);
            }
            else if (_sResult.Count > 1)
            {
                _result = _sResult[0].Substring(0, 1) + _sResult[_sResult.Count - 1].Substring(0, 1);
            }
            else
            {
                _result = "?";
            }
            return _result.ToUpper();
        }

        public static byte[] GenerateAvatarImage(string text, string color = "#7d8c75")
        {
            // Define colors
            Color fontColor = ColorTranslator.FromHtml("#FFF");
            Color bgColor = ColorTranslator.FromHtml(color);
            Font font = new Font("Arial", 45, FontStyle.Regular);

            // Create a dummy image to measure text size
            using (Image img = new Bitmap(1, 1))
            using (Graphics drawing = Graphics.FromImage(img))
            {
                SizeF textSize = drawing.MeasureString(text, font);

                // Create the final image
                using (Bitmap finalImg = new Bitmap(110, 110))
                using (Graphics finalDrawing = Graphics.FromImage(finalImg))
                {
                    // Clear background
                    finalDrawing.Clear(bgColor);

                    // Set up text drawing
                    StringFormat stringFormat = new StringFormat
                    {
                        Alignment = StringAlignment.Center,
                        FormatFlags = StringFormatFlags.LineLimit,
                        Trimming = StringTrimming.Character
                    };

                    // Draw the text
                    finalDrawing.DrawString(text, font, new SolidBrush(fontColor), new Rectangle(0, 20, 110, 110), stringFormat);

                    // Save to memory stream
                    using (MemoryStream memoryStream = new MemoryStream())
                    {
                        finalImg.Save(memoryStream, ImageFormat.Jpeg);
                        return memoryStream.ToArray(); // Return byte array
                    }
                }
            }
        }







        public static string Encrypt(string plainText)
        {
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Convert.FromBase64String(_encryptionKey);  // Encryption Key
                aesAlg.IV = new byte[16]; // 16 bytes for AES IV (can be randomized for better security)

                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);
                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {
                            swEncrypt.Write(plainText);
                        }
                    }
                    return Convert.ToBase64String(msEncrypt.ToArray());
                }
            }
        }

        public static string Decrypt(string cipherText)
        {
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Convert.FromBase64String(_encryptionKey);  // Encryption Key
                aesAlg.IV = new byte[16]; // 16 bytes for AES IV (must be the same as used in encryption)

                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);
                using (MemoryStream msDecrypt = new MemoryStream(Convert.FromBase64String(cipherText)))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                        {
                            return srDecrypt.ReadToEnd();
                        }
                    }
                }
            }
        }



    }
}
