using System.Net;
using Xunit;

namespace Tailbook.Api.Tests.TestSupport.Http;

public static class ApiResponseAssertions
{
    extension(HttpResponseMessage response)
    {
        public void ShouldHaveStatus(HttpStatusCode expectedStatus)
            => Assert.Equal(expectedStatus, response.StatusCode);

        public void ShouldBeOk()
            => response.ShouldHaveStatus(HttpStatusCode.OK);

        public void ShouldBeCreated()
            => response.ShouldHaveStatus(HttpStatusCode.Created);

        public void ShouldBeBadRequest()
            => response.ShouldHaveStatus(HttpStatusCode.BadRequest);

        public void ShouldBeUnauthorized()
            => response.ShouldHaveStatus(HttpStatusCode.Unauthorized);

        public void ShouldBeForbidden()
            => response.ShouldHaveStatus(HttpStatusCode.Forbidden);

        public void ShouldBeNotFound()
            => response.ShouldHaveStatus(HttpStatusCode.NotFound);

        public void ShouldBeConflict()
            => response.ShouldHaveStatus(HttpStatusCode.Conflict);
    }
}
