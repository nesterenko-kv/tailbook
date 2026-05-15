namespace Tailbook.Api.Tests.TestSupport.Auth;

internal static class AuthenticatedClientFactory
{
    extension(CustomWebApplicationFactory factory)
    {
        internal HttpClient CreateAnonymousClient()
            => factory.CreateClient();

        internal async Task<HttpClient> CreateAdminClientAsync()
            => await factory.CreateAuthenticatedClientAsync(TestUsers.AdminEmail, TestUsers.AdminPassword);

        internal async Task<HttpClient> CreateAuthenticatedClientAsync(string email,
            string password)
        {
            var token = await factory.LoginAsAsync(email, password);
            var client = factory.CreateClient();
            CustomWebApplicationFactory.SetBearer(client, token);
            return client;
        }

        internal async Task<HttpClient> CreateClientForRoleAsync(string email,
            string displayName,
            string password,
            params string[] roleCodes)
        {
            await factory.SeedUserAsync(email, displayName, password, roleCodes);
            return await factory.CreateAuthenticatedClientAsync(email, password);
        }
    }
}
