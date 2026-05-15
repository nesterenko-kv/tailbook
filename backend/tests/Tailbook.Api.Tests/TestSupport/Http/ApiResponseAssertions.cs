using System.Net;
using Xunit;

namespace Tailbook.Api.Tests.TestSupport.Http;

internal static class ApiResponseAssertions
{
    extension(HttpResponseMessage response)
    {
        internal void ShouldHaveStatus(HttpStatusCode expectedStatus)
            => Assert.Equal(expectedStatus, response.StatusCode);

        internal void ShouldBeOk()
            => response.ShouldHaveStatus(HttpStatusCode.OK);

        internal void ShouldBeCreated()
            => response.ShouldHaveStatus(HttpStatusCode.Created);

        internal void ShouldBeBadRequest()
            => response.ShouldHaveStatus(HttpStatusCode.BadRequest);

        internal void ShouldBeUnauthorized()
            => response.ShouldHaveStatus(HttpStatusCode.Unauthorized);

        internal void ShouldBeForbidden()
            => response.ShouldHaveStatus(HttpStatusCode.Forbidden);

        internal void ShouldBeNotFound()
            => response.ShouldHaveStatus(HttpStatusCode.NotFound);

        internal void ShouldBeConflict()
            => response.ShouldHaveStatus(HttpStatusCode.Conflict);
    }
}
