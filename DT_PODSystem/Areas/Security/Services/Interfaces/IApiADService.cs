using System.Collections.Generic;
using System.Net.Mail;
using System.Threading.Tasks;
using DT_PODSystem.Areas.Security.Models.DTOs;
using DT_PODSystem.Areas.Security.Models.ViewModels;

namespace DT_PODSystem.Areas.Security.Services.Interfaces
{
    public interface IApiADService
    {

        #region Queue Email Sending

        //To Do :

        #endregion



        #region Direct Email Sending


        Task<ADUserDetails> GetADUserAsync(string username);
        Task<List<ADUserDetails>> SearchADUsersAsync(string searchKey);
        Task<ADUserDetails> AuthenticateADUserAsync(string username, string password);
        Task<string> GetTokenAsync();

        #endregion

    }
}