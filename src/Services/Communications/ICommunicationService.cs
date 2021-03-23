using System.Threading.Tasks;
using AlbedoTeam.Identity.Contracts.Commands;
using AlbedoTeam.Identity.Contracts.Requests;
using MassTransit;

namespace Identity.Business.Users.Services.Communications
{
    public interface ICommunicationService
    {
        Task SendUserActivatedEmail(ConsumeContext<ActivateUser> context, string name, string email);
        Task SendUserDeactivatedEmail(ConsumeContext<DeactivateUser> context, string name, string email);
        Task SendUserCreatedEmail(ConsumeContext<CreateUser> context, string name, string email, string login);
        Task SendPasswordChangedEmail(ConsumeContext<ChangeUserPassword> context, string name, string email);
        Task SendPasswordChangedEmail(ConsumeContext<SetUserPassword> context, string name, string email);

        Task SendPasswordChangeRequestedEmail(ConsumeContext<RequestPasswordChange> context, string name, string email,
            string token);
    }
}