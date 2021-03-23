using AlbedoTeam.Identity.Contracts.Responses;
using Identity.Business.Users.Models;

namespace Identity.Business.Users.Mappers.Abstractions
{
    public interface IPasswordRecoveryMapper
    {
        // request to model

        // model to response
        PasswordRecoveryResponse MapModelToResponse(PasswordRecovery model);
    }
}