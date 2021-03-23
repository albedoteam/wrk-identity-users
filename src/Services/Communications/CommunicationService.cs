using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using AlbedoTeam.Communications.Contracts.Commands;
using AlbedoTeam.Identity.Contracts.Commands;
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
        private readonly ILogger<CommunicationService> _logger;
        private readonly IRequestClient<ListAuthServers> _client;
        private readonly IMemoryCache _cache;

        public CommunicationService(
            IRequestClient<ListAuthServers> client,
            IMemoryCache cache,
            ILogger<CommunicationService> logger)
        {
            _client = client;
            _cache = cache;
            _logger = logger;
        }

        public async Task SendUserActivatedEmail(ConsumeContext<ActivateUser> context, string name, string email)
        {
            var rule = await GetCommunicationRule(context.Message.AccountId, CommunicationEvent.OnUserActivated);

            var redirectUrl = "";
            rule.DefaultContentParameters?.TryGetValue("redirectUrl", out redirectUrl);

            await context.Send<SendMessage>(new
            {
                context.Message.AccountId,
                rule.TemplateId,
                Subject = "Usuário Ativado",
                Destinations = new[]
                {
                    new
                    {
                        Name = name,
                        Address = email
                    }
                },
                Parameters = new[] {
                    new
                    {
                        Key = "username",
                        Value = name
                    },
                    new
                    {
                        Key = "redirectUrl",
                        Value = redirectUrl
                    }
                }
            });
        }

        public async Task SendUserDeactivatedEmail(ConsumeContext<DeactivateUser> context, string name, string email)
        {
            var rule = await GetCommunicationRule(context.Message.AccountId, CommunicationEvent.OnUserDeactivated);

            var redirectUrl = "";
            rule.DefaultContentParameters?.TryGetValue("redirectUrl", out redirectUrl);

            await context.Send<SendMessage>(new
            {
                context.Message.AccountId,
                rule.TemplateId,
                Subject = "Usuário Desativado",
                Destinations = new[]
                {
                    new
                    {
                        Name = name,
                        Address = email
                    }
                },
                Parameters = new[] {
                    new
                    {
                        Key = "username",
                        Value = name
                    },
                    new
                    {
                        Key = "redirectUrl",
                        Value = redirectUrl
                    }
                }
            });
        }

        public async Task SendUserCreatedEmail(ConsumeContext<CreateUser> context, string name, string email,
            string login)
        {
            var rule = await GetCommunicationRule(context.Message.AccountId, CommunicationEvent.OnUserCreated);

            var redirectUrl = "";
            rule.DefaultContentParameters?.TryGetValue("redirectUrl", out redirectUrl);

            await context.Send<SendMessage>(new
            {
                context.Message.AccountId,
                rule.TemplateId,
                Subject = "Bem vindo!",
                Destinations = new[]
                {
                    new
                    {
                        Name = name,
                        Address = email
                    }
                },
                Parameters = new[] {
                    new
                    {
                        Key = "username",
                        Value = name
                    },
                    new
                    {
                        Key = "login",
                        Value = login
                    },
                    new
                    {
                        Key = "redirectUrl",
                        Value = redirectUrl
                    }
                }
            });
        }

        public async Task SendPasswordChangedEmail(ConsumeContext<ChangeUserPassword> context, string name,
            string email)
        {
            var rule = await GetCommunicationRule(context.Message.AccountId,
                CommunicationEvent.OnPasswordChanged);

            var redirectUrl = "";
            rule.DefaultContentParameters?.TryGetValue("redirectUrl", out redirectUrl);

            await context.Send<SendMessage>(new
            {
                context.Message.AccountId,
                rule.TemplateId,
                Subject = "Redefinição de senha realizada",
                Destinations = new[]
                {
                    new
                    {
                        Name = name,
                        Address = email
                    }
                },
                Parameters = new[] {
                    new
                    {
                        Key = "username",
                        Value = name
                    },
                    new
                    {
                        Key = "redirectUrl",
                        Value = redirectUrl
                    }
                }
            });
        }

        public async Task SendPasswordChangedEmail(ConsumeContext<SetUserPassword> context, string name,
            string email)
        {
            var rule = await GetCommunicationRule(context.Message.AccountId,
                CommunicationEvent.OnPasswordChanged);

            var redirectUrl = "";
            rule.DefaultContentParameters?.TryGetValue("redirectUrl", out redirectUrl);

            await context.Send<SendMessage>(new
            {
                context.Message.AccountId,
                rule.TemplateId,
                Subject = "Redefinição de senha realizada",
                Destinations = new[]
                {
                    new
                    {
                        Name = name,
                        Address = email
                    }
                },
                Parameters = new[] {
                    new
                    {
                        Key = "username",
                        Value = name
                    },
                    new
                    {
                        Key = "redirectUrl",
                        Value = redirectUrl
                    }
                }
            });
        }

        public async Task SendPasswordChangeRequestedEmail(ConsumeContext<RequestPasswordChange> context, string name,
            string email, string token)
        {
            var rule = await GetCommunicationRule(context.Message.AccountId,
                CommunicationEvent.OnPasswordChangeRequested);

            var redirectUrl = "";
            rule.DefaultContentParameters?.TryGetValue("redirectUrl", out redirectUrl);

            await context.Send<SendMessage>(new
            {
                context.Message.AccountId,
                rule.TemplateId,
                Subject = "Solicitação de redefinição de senha",
                Destinations = new[]
                {
                    new
                    {
                        Name = name,
                        Address = email
                    }
                },
                Parameters = new[] {
                    new
                    {
                        Key = "username",
                        Value = name
                    },
                    new
                    {
                        Key = "token",
                        Value = token
                    },
                    new
                    {
                        Key = "redirectUrl",
                        Value = redirectUrl
                    }
                }
            });
        }

        private async Task<ICommunicationRule> GetCommunicationRule(string accountId, CommunicationEvent @event)
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