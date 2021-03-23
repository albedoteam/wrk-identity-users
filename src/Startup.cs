using AlbedoTeam.Accounts.Contracts.Requests;
using AlbedoTeam.Communications.Contracts.Commands;
using AlbedoTeam.Identity.Contracts.Events;
using AlbedoTeam.Identity.Contracts.Requests;
using AlbedoTeam.Sdk.DataLayerAccess;
using AlbedoTeam.Sdk.JobWorker.Configuration.Abstractions;
using AlbedoTeam.Sdk.MessageConsumer;
using Identity.Business.Users.Consumers.PasswordRecoveryConsumers;
using Identity.Business.Users.Consumers.UserConsumers;
using Identity.Business.Users.Db;
using Identity.Business.Users.Mappers;
using Identity.Business.Users.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Identity.Business.Users
{
    public class Startup : IWorkerConfigurator
    {
        public void Configure(IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton(new IdentityServerOptions
            {
                OrgUrl = configuration.GetValue<string>("IdentityServer_OrgUrl"),
                PandorasClientId = configuration.GetValue<string>("IdentityServer_ClientId"),
                ApiUrl = configuration.GetValue<string>("IdentityServer_ApiUrl"),
                ApiKey = configuration.GetValue<string>("IdentityServer_ApiKey")
            });

            services.AddDataLayerAccess(db =>
            {
                db.ConnectionString = configuration.GetValue<string>("DatabaseSettings_ConnectionString");
                db.DatabaseName = configuration.GetValue<string>("DatabaseSettings_DatabaseName");
            });

            services.AddMappers();
            services.AddRepositories();
            services.AddServices();
            services.AddTransient<IJobRunner, JobConsumer>();
            services.AddMemoryCache();

            services.AddBroker(
                configure => configure
                    .SetBrokerOptions(broker => broker.Host = configuration.GetValue<string>("Broker_Host")),
                consumers =>
                {
                    // users
                    consumers
                        .Add<CreateUserConsumer>()
                        .Add<DeleteUserConsumer>()
                        .Add<UpdateUserConsumer>()
                        .Add<GetUserConsumer>()
                        .Add<ListUsersConsumer>()
                        .Add<ActivateUserConsumer>()
                        .Add<DeactivateUserConsumer>()
                        .Add<AddGroupToUserConsumer>()
                        .Add<RemoveGroupFromUserConsumer>()
                        .Add<SetUserPasswordConsumer>()
                        .Add<ChangeUserPasswordConsumer>()
                        .Add<ClearUserSessionsConsumer>()
                        .Add<ExpireUserPasswordConsumer>()
                        .Add<ChangeUserTypeOnUserConsumer>();

                    // pwd recovery
                    consumers
                        .Add<GetPasswordRecoveryConsumer>()
                        .Add<RequestPasswordChangeConsumer>();
                },
                queues =>
                {
                    // user events
                    queues
                        .Map<UserActivated>()
                        .Map<UserDeactivated>()
                        .Map<UserPasswordChanged>()
                        .Map<UserPasswordExpired>()
                        .Map<UserPasswordSetted>()
                        .Map<UserSessionsCleared>()
                        .Map<UserTypeChangedOnUser>()
                        .Map<GroupAddedToUser>()
                        .Map<GroupRemovedFromUser>();

                    // pwd recovery events
                    queues
                        .Map<UserPasswordChangeRequested>();

                    // communication commands
                    queues
                        .Map<SendMessage>();
                },
                clients => clients
                    .Add<GetAccount>()
                    .Add<GetGroup>()
                    .Add<GetUserType>()
                    .Add<ListAuthServers>()
            );
        }
    }
}