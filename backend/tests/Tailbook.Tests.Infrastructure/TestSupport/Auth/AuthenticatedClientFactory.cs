using Tailbook.Api.Tests;

namespace Tailbook.Api.Tests.TestSupport.Auth;

public static class AuthenticatedClientFactory
{
    extension(RealDbWebApplicationFactory factory)
    {
        public HttpClient CreateAnonymousClient()
            => factory.CreateClient();

        public async Task<HttpClient> CreateAdminClientAsync()
            => await factory.CreateAuthenticatedClientAsync(TestUsers.AdminEmail, TestUsers.AdminPassword);

        public async Task<HttpClient> CreateAuthenticatedClientAsync(string email,
            string password)
        {
            var token = await factory.LoginAsAsync(email, password);
            var client = factory.CreateClient();
            RealDbWebApplicationFactory.SetBearer(client, token);
            return client;
        }

        public async Task<HttpClient> CreateClientForRoleAsync(string email,
            string displayName,
            string password,
            params string[] roleCodes)
        {
            await factory.SeedUserAsync(email, displayName, password, roleCodes);
            return await factory.CreateAuthenticatedClientAsync(email, password);
        }
    }
}
