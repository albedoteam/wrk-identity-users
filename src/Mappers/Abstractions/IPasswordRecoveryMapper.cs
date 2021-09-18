namespace Identity.Business.Users.Mappers.Abstractions
{
    using AlbedoTeam.Identity.Contracts.Responses;
    using Models;

    public interface IPasswordRecoveryMapper
    {
        // request to model

        // model to response
        PasswordRecoveryResponse MapModelToResponse(PasswordRecovery model);
    }
}