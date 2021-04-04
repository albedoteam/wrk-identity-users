namespace Identity.Business.Users.Services.Accounts
{
    using System.Threading.Tasks;
    using AlbedoTeam.Accounts.Contracts.Responses;

    public interface IAccountService
    {
        Task<bool> IsAccountValid(string accountId);

        Task<AccountResponse> GetAccount(string accountId);
    }
}