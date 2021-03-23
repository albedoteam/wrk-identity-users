using System;
using System.Linq;
using System.Threading.Tasks;
using AlbedoTeam.Identity.Contracts.Common;
using AlbedoTeam.Identity.Contracts.Requests;
using AlbedoTeam.Identity.Contracts.Responses;
using Identity.Business.Users.Db.Abstractions;
using Identity.Business.Users.Mappers.Abstractions;
using MassTransit;

namespace Identity.Business.Users.Consumers.PasswordRecoveryConsumers
{
    public class GetPasswordRecoveryConsumer : IConsumer<GetPasswordRecovery>
    {
        private readonly IPasswordRecoveryRepository _repository;
        private readonly IPasswordRecoveryMapper _mapper;

        public GetPasswordRecoveryConsumer(
            IPasswordRecoveryRepository repository,
            IPasswordRecoveryMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task Consume(ConsumeContext<GetPasswordRecovery> context)
        {
            if (!context.Message.AccountId.IsValidObjectId())
                await context.RespondAsync<ErrorResponse>(new
                {
                    ErrorType = ErrorType.InvalidOperation,
                    ErrorMessage = "The Account ID does not have a valid ObjectId format"
                });

            var passwordRecovery = (await _repository.FilterBy(
                context.Message.AccountId,
                p => p.ValidationToken == context.Message.Token)).FirstOrDefault();

            if (passwordRecovery is null || DateTime.UtcNow > passwordRecovery.ExpiresAt)
                await context.RespondAsync<ErrorResponse>(new
                {
                    ErrorType = ErrorType.InvalidOperation,
                    ErrorMessage = "Invalid validation token"
                });
            else
                await context.RespondAsync(_mapper.MapModelToResponse(passwordRecovery));
        }
    }
}