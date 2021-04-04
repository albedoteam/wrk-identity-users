namespace Identity.Business.Users.Consumers.UserConsumers
{
    using System.Linq;
    using System.Threading.Tasks;
    using AlbedoTeam.Identity.Contracts.Common;
    using AlbedoTeam.Identity.Contracts.Requests;
    using AlbedoTeam.Identity.Contracts.Responses;
    using Db.Abstractions;
    using Mappers.Abstractions;
    using MassTransit;
    using Models;
    using MongoDB.Driver;

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
            var page = context.Message.Page > 0 ? context.Message.Page : 1;
            var pageSize = context.Message.PageSize <= 1 ? 1 : context.Message.PageSize;

            var filterBy = CreateFilters(
                context.Message.ShowDeleted,
                null,
                AddFilterBy(context.Message.FilterBy));

            var orderBy = _repository.Helpers.CreateSorting(
                context.Message.OrderBy,
                context.Message.Sorting.ToString());

            var (totalPages, users) = await _repository.QueryByPage(
                context.Message.AccountId,
                page,
                pageSize,
                filterBy,
                orderBy);

            if (!users.Any())
                await context.RespondAsync<ErrorResponse>(new
                {
                    ErrorType = ErrorType.NotFound,
                    ErrorMessage = "Users not found"
                });
            else
                await context.RespondAsync<ListUsersResponse>(new
                {
                    context.Message.Page,
                    context.Message.PageSize,
                    RecordsInPage = users.Count,
                    TotalPages = totalPages,
                    Items = _mapper.MapModelToResponse(users.ToList()),
                    context.Message.FilterBy,
                    context.Message.OrderBy,
                    context.Message.Sorting
                });
        }

        private FilterDefinition<User> AddFilterBy(string filterBy)
        {
            if (string.IsNullOrWhiteSpace(filterBy))
                return null;

            var optionalFilters = Builders<User>.Filter.Or(
                _repository.Helpers.Like(a => a.FirstName, filterBy),
                _repository.Helpers.Like(a => a.LastName, filterBy),
                _repository.Helpers.Like(a => a.Username, filterBy),
                _repository.Helpers.Like(a => a.Email, filterBy),
                _repository.Helpers.Like(a => a.UsernameAtProvider, filterBy)
            );

            return optionalFilters;
        }

        private static FilterDefinition<User> CreateFilters(
            bool showDeleted = false,
            FilterDefinition<User> requiredFields = null,
            FilterDefinition<User> filteredByFilters = null)
        {
            var mainFilters = Builders<User>.Filter.And(Builders<User>.Filter.Empty);

            if (!showDeleted)
                mainFilters &= Builders<User>.Filter.Eq(a => a.IsDeleted, false);

            if (requiredFields is { })
                mainFilters &= requiredFields;

            if (filteredByFilters is { })
                mainFilters &= filteredByFilters;

            return mainFilters;
        }
    }
}