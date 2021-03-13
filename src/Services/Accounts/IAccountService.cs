using System.Threading.Tasks;
using AlbedoTeam.Accounts.Contracts.Responses;

namespace Identity.Business.Users.Services.Accounts
{
    public interface IAccountService
    {
        Task<bool> IsAccountValid(string accountId);

        Task<AccountResponse> GetAccount(string accountId);
    }
}