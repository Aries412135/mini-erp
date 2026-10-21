using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using MiniErp.Application.Feature.Users.SetUserRoles;
using NSubstitute;

namespace MiniErp.Api.IntegrationTests;

public class AuthorizationTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public AuthorizationTests(ApiFactory factory)
    {
        _factory = factory;
        _factory.Sender.ClearReceivedCalls();
    }

    private HttpClient CreateClient(string? token = null)
    {
        var client = _factory.CreateClient();

        if (token is not null)
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return client;
    }

    private static string RolesUrl() => $"/api/users/{Guid.NewGuid()}/roles";
    private static object RolesBody() => new { roles = new[] { "Sales" } };

    [Fact]
    public async Task Health_WithoutToken_Returns200()
    {
        var response  = await CreateClient().GetAsync("/api/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithEmptyBody_Returns400_NotUnauthorized()
    {
        var response = await CreateClient().PostAsJsonAsync("/api/auth/login", new { });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Me_WithoutToken_Returns401_WithProblemDetails()
    {
        var response = await CreateClient().GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(401, body.GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task Me_WithValidToken_Returns200()
    {
        var token = TestTokens.Create(new[] { "Warehouse" });

        var response = await CreateClient(token).GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Me_WithTokenSignedByOtherKey_Returns401()
    {
        var token = TestTokens.Create(new[] { "Admin" }, secret: "another-secret-key-0123546789-abcdefghij");

        var response = await CreateClient(token).GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_WithExpiredToken_Returns401()
    {
        var token = TestTokens.Create(new[] { "Admin" }, expiresAtUtc: DateTime.UtcNow.AddMinutes(-5));

        var response = await CreateClient(token).GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SetRoles_WithoutToken_Returns401()
    {
        var response = await CreateClient().PutAsJsonAsync(RolesUrl(), RolesBody());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SetRoles_AsNonAdmin_Returns403_AndNeverReachesUseCase()
    {
        var token = TestTokens.Create(new[] { "Warehouse" });

        var response = await CreateClient(token).PutAsJsonAsync(RolesUrl(), RolesBody());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        await _factory.Sender.DidNotReceive()
            .Send(Arg.Any<SetUserRolesCommand>(), Arg.Any<CancellationToken>());
    }

     [Fact]
    public async Task SetRoles_AsAdmin_Returns204_AndReachesUseCase()
    {
        var token = TestTokens.Create(new[] { "Admin" });

        var response = await CreateClient(token).PutAsJsonAsync(RolesUrl(), RolesBody());

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        await _factory.Sender.Received(1)
            .Send(Arg.Any<SetUserRolesCommand>(), Arg.Any<CancellationToken>());
    }

    // [Fact]
    // public async Task Debug_Authz()
    // {
    //     var token = TestTokens.Create(new[] {"Admin"});

    //     var response = await CreateClient(token).GetAsync("/api/auth/authz-debug");

    //     Assert.Fail(await response.Content.ReadAsStringAsync());
    // }
}