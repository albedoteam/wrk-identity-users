using System.Threading.Tasks;
using AlbedoTeam.Identity.Contracts.Common;

namespace Identity.Business.Users.Services.Communications
{
    public interface ICommunicationService
    {
        string FormatRedirectUrl(ICommunicationRule rule, string accountId);
        Task<ICommunicationRule> GetCommunicationRule(string accountId, CommunicationEvent @event);
    }
}