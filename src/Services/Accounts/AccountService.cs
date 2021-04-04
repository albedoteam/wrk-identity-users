namespace Identity.Business.Users.Services.Accounts
{
    using System.Threading.Tasks;
    using AlbedoTeam.Accounts.Contracts.Requests;
    using AlbedoTeam.Accounts.Contracts.Responses;
    using MassTransit;

    public class AccountService : IAccountService
    {
        private readonly IRequestClient<GetAccount> _client;

        public AccountService(IRequestClient<GetAccount> client)
        {
            _client = client;
        }

        public async Task<bool> IsAccountValid(string accountId)
        {
            var account = await GetAccountRequest(accountId);
            return account is { } && account.Enabled;
        }

        public async Task<AccountResponse> GetAccount(string accountId)
        {
            return await GetAccountRequest(accountId);
        }

        private async Task<AccountResponse> GetAccountRequest(string accountId)
        {
            if (!accountId.IsValidObjectId())
                return null;

            var (accountResponse, notFoundResponse) = await _client.GetResponse<AccountResponse, ErrorResponse>(new
            {
                Id = accountId,
                ShowDeleted = false
            });

            if (accountResponse.IsCompletedSuccessfully)
            {
                var account = await accountResponse;
                return account.Message;
            }

            await notFoundResponse;
            return null;
        }
    }
}