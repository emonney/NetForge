using System.Net;
using System.Net.Http.Json;
using NetForge.Server.Features.Auth;
using NetForge.Tests.Integration.Fixtures;
using Shouldly;

namespace NetForge.Tests.Integration.Features.Auth;

/// <summary>
/// The real credential path through ASP.NET Identity's <c>SignInManager</c> (the integration tests'
/// authorization uses <see cref="TestAuthHandler"/>; this is where the actual login is exercised). Wrong
/// credentials surface a typed 401 ProblemDetails; correct credentials return the signed-in identity.
/// </summary>
[Collection(IntegrationCollection.Name)]
public sealed class LoginFlowTests(CustomWebApplicationFactory factory)
{
    [Fact]
    public async Task Wrong_password_returns_401_with_invalid_credentials_code()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest(CustomWebApplicationFactory.LoginEmail, "definitely-not-the-password"));

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        var problem = await response.Content.ReadFromJsonAsync<ProblemResponse>();
        problem!.Code.ShouldBe("INVALID_CREDENTIALS");
    }

    [Fact]
    public async Task Unknown_email_also_returns_invalid_credentials_not_a_distinct_code()
    {
        // No account enumeration — an unknown address looks identical to a wrong password.
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest("nobody@netforge.test", "whatever-Pass1"));

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await response.Content.ReadFromJsonAsync<ProblemResponse>())!.Code.ShouldBe("INVALID_CREDENTIALS");
    }

    [Fact]
    public async Task Correct_credentials_sign_in_and_return_the_user()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest(CustomWebApplicationFactory.LoginEmail, CustomWebApplicationFactory.LoginPassword));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<LoginResultDto>();
        result.ShouldNotBeNull();
        result!.RequiresTwoFactor.ShouldBeFalse();
        result.User!.Email.ShouldBe(CustomWebApplicationFactory.LoginEmail);
    }
}
