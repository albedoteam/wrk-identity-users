using System.Threading.Tasks;
using AlbedoTeam.Identity.Contracts.Common;
using AlbedoTeam.Identity.Contracts.Requests;
using AlbedoTeam.Identity.Contracts.Responses;
using Identity.Business.Users.Db.Abstractions;
using Identity.Business.Users.Mappers.Abstractions;
using MassTransit;

namespace Identity.Business.Users.Consumers.UserConsumers
{
    public class GetUserConsumer : IConsumer<GetUser>
    {
        private readonly IUserMapper _mapper;
        private readonly IUserRepository _repository;

        public GetUserConsumer(IUserMapper mapper, IUserRepository repository)
        {
            _mapper = mapper;
            _repository = repository;
        }

        public async Task Consume(ConsumeContext<GetUser> context)
        {
            if (!context.Message.Id.IsValidObjectId())
                await context.RespondAsync<ErrorResponse>(new
                {
                    ErrorType = ErrorType.InvalidOperation,
                    ErrorMessage = "The User ID does not have a valid ObjectId format"
                });

            var user = await _repository.FindById(
                context.Message.AccountId,
                context.Message.Id,
                context.Message.ShowDeleted);

            if (user is null)
                await context.RespondAsync<ErrorResponse>(new
                {
                    ErrorType = ErrorType.NotFound,
                    ErrorMessage = "User not found"
                });
            else
                await context.RespondAsync(_mapper.MapModelToResponse(user));
        }
    }
}