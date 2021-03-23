using Identity.Business.Users.Services.Accounts;
using Identity.Business.Users.Services.Communications;
using Identity.Business.Users.Services.IdentityServers;
using Identity.Business.Users.Services.IdentityServers.Abstractions;
using Identity.Business.Users.Services.IdentityServers.Providers.Okta;
using Microsoft.Extensions.DependencyInjection;

namespace Identity.Business.Users.Services
{
    public static class Setup
    {
        public static IServiceCollection AddServices(this IServiceCollection services)
        {
            services.AddScoped<IAccountService, AccountService>();
            services.AddScoped<ICommunicationService, CommunicationService>();

            // identity server service and factory
            services.AddScoped<IIdentityServerService, IdentityServerService>();
            services.AddScoped<IdentityProviderFactory>();

            // providers
            services
                .AddScoped<OktaUserProvider>()
                .AddScoped<IUserProvider, OktaUserProvider>(s => s.GetService<OktaUserProvider>());

            return services;
        }
    }
}