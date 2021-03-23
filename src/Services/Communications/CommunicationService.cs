using System;
using System.Threading.Tasks;
using AlbedoTeam.Identity.Contracts.Common;
using AlbedoTeam.Identity.Contracts.Requests;
using AlbedoTeam.Identity.Contracts.Responses;
using MassTransit;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Identity.Business.Users.Services.Communications
{
    public class CommunicationService : ICommunicationService
    {
        private readonly IMemoryCache _cache;
        private readonly IRequestClient<ListAuthServers> _client;
        private readonly ILogger<CommunicationService> _logger;

        public CommunicationService(
            IRequestClient<ListAuthServers> client,
            IMemoryCache cache,
            ILogger<CommunicationService> logger)
        {
            _client = client;
            _cache = cache;
            _logger = logger;
        }


        public string FormatRedirectUrl(ICommunicationRule rule, string accountId)
        {
            var redirectUrl = "";
            rule.DefaultContentParameters?.TryGetValue("redirectUrl", out redirectUrl);
            if (string.IsNullOrWhiteSpace(redirectUrl))
                return "";

            if (redirectUrl.Contains("?"))
                return redirectUrl + $"&account={accountId}";

            return redirectUrl + $"?account={accountId}";
        }

        public async Task<ICommunicationRule> GetCommunicationRule(string accountId, CommunicationEvent @event)
        {
            if (!_cache.TryGetValue(CacheKeys.AuthServerId, out AuthServerResponse authServerCached))
            {
                authServerCached = await RequestAuthServer(accountId);

                var cacheOptions = new MemoryCacheEntryOptions()
                    .SetSlidingExpiration(TimeSpan.FromHours(1));

                _cache.Set(CacheKeys.AuthServerId, authServerCached, cacheOptions);
            }

            if (authServerCached.CommunicationRules is null)
            {
                _logger.LogWarning("Communication Rules not found for account {AccountId}", accountId);
                return null;
            }

            switch (@event)
            {
                case CommunicationEvent.OnUserCreated:
                    return authServerCached.CommunicationRules.OnUserCreated;
                case CommunicationEvent.OnPasswordChangeRequested:
                    return authServerCached.CommunicationRules.OnPasswordChangeRequested;
                case CommunicationEvent.OnPasswordChanged:
                    return authServerCached.CommunicationRules.OnPasswordChanged;
                case CommunicationEvent.OnUserActivated:
                    return authServerCached.CommunicationRules.OnUserActivated;
                case CommunicationEvent.OnUserDeactivated:
                    return authServerCached.CommunicationRules.OnUserDeactivated;
                default:
                    _logger.LogWarning("Communication Rule {Rule} not found for account {AccountId}", @event,
                        accountId);
                    return null;
            }
        }

        private async Task<AuthServerResponse> RequestAuthServer(string accountId)
        {
            var (successResponse, errorResoponse) = await _client.GetResponse<ListAuthServersResponse, ErrorResponse>(
                new
                {
                    ShowDeleted = false,
                    Page = 1,
                    PageSize = 1,
                    FilterBy = accountId,
                    OrderBy = "accountId",
                    Sorting = Sorting.Asc
                });

            if (successResponse.IsCompletedSuccessfully)
            {
                var list = await successResponse;
                return list.Message.RecordsInPage == 1 ? list.Message.Items[0] : null;
            }

            await errorResoponse;
            return null;
        }
    }
}