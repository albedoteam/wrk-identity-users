namespace Identity.Business.Users.Services.IdentityServers.Abstractions
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public interface IUserProvider
    {
        Task<string> Create(
            string accountName,
            string userTypeProviderId,
            string firstName,
            string lastName,
            string login,
            List<string> groupIds);

        Task<bool> Update(
            string userProviderId,
            string firstName,
            string lastName);

        Task Delete(string userProviderId);

        Task<string> Activate(string userProviderId);
        Task Deactivate(string userProviderId);
        Task AddGroup(string userProviderId, string groupProviderId);
        Task RemoveGroup(string userProviderId, string groupProviderId);
        Task<bool> ChangePassword(string userProviderId, string currentPassword, string newPassword);
        Task<string> ExpirePasswordAndSetTemporaryOne(string userProviderId);
        Task<bool> SetPassword(string userProviderId, string newPassword);
        Task ClearSessions(string userProviderId);
        Task<bool> ChangeUserType(string userProviderId, string userTypeProviderId);
    }
}