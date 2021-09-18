namespace Identity.Business.Users.Services
{
    using Accounts;
    using Communications;
    using IdentityServers;
    using IdentityServers.Abstractions;
    using IdentityServers.Providers.Okta;
    using Microsoft.Extensions.DependencyInjection;

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