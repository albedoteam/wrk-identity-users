namespace Identity.Business.Users.Services.Communications
{
    using System.Threading.Tasks;
    using AlbedoTeam.Identity.Contracts.Common;

    public interface ICommunicationService
    {
        string FormatRedirectUrl(ICommunicationRule rule, string accountId);
        Task<ICommunicationRule> GetCommunicationRule(string accountId, CommunicationEvent @event);
    }
}