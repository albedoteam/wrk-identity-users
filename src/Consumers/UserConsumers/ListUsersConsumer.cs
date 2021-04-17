namespace Identity.Business.Users.Consumers.UserConsumers
{
    using System.Linq;
    using System.Threading.Tasks;
    using AlbedoTeam.Identity.Contracts.Common;
    using AlbedoTeam.Identity.Contracts.Requests;
    using AlbedoTeam.Identity.Contracts.Responses;
    using AlbedoTeam.Sdk.DataLayerAccess.Utils.Query;
    using Db.Abstractions;
    using Mappers.Abstractions;
    using MassTransit;
    using Models;

    public class ListUsersConsumer : IConsumer<ListUsers>
    {
        private readonly IUserMapper _mapper;
        private readonly IUserRepository _repository;

        public ListUsersConsumer(IUserMapper mapper, IUserRepository repository)
        {
            _mapper = mapper;
            _repository = repository;
        }

        public async Task Consume(ConsumeContext<ListUsers> context)
        {
            var queryRequest = QueryUtils.GetQueryParams<User>(_mapper.RequestToQuery(context.Message));
            var queryResponse = await _repository.QueryByPage(context.Message.AccountId, queryRequest);

            if (!queryResponse.Records.Any())
                await context.RespondAsync<ErrorResponse>(new
                {
                    ErrorType = ErrorType.NotFound,
                    ErrorMessage = "Users not found"
                });
            else
                await context.RespondAsync<ListUsersResponse>(new
                {
                    queryResponse.Page,
                    queryResponse.PageSize,
                    queryResponse.RecordsInPage,
                    queryResponse.TotalPages,
                    Items = _mapper.MapModelToResponse(queryResponse.Records.ToList()),
                    context.Message.FilterBy,
                    context.Message.OrderBy,
                    context.Message.Sorting
                });
        }
    }
}