using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PayAhead.Api;
using Xunit;

namespace PayAhead.IntegrationTests;

public sealed class ApiPipelineTests
{
    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    public void Actual_api_refuses_development_identity_outside_development(string environment)
    {
        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder
            .UseEnvironment(environment)
            .UseSetting("DevelopmentIdentity:Enabled", "true")
            .UseSetting("DevelopmentIdentity:UserId", Guid.NewGuid().ToString()));
        var error = Assert.ThrowsAny<Exception>(() => factory.CreateClient());
        Assert.Contains("yalnızca Development", error.ToString());
    }

    [Fact]
    public async Task Actual_api_can_explicitly_enable_development_identity()
    {
        await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder
            .UseEnvironment("Development").UseRegressionPremium().UseTestIdentity()
            .UseSetting("DevelopmentIdentity:Enabled", "true")
            .UseSetting("DevelopmentIdentity:UserId", Guid.NewGuid().ToString()));
        using var client = factory.CreateClient();
        var schemes = factory.Services.GetRequiredService<IAuthenticationSchemeProvider>();
        Assert.Equal("TestIdentity", (await schemes.GetDefaultAuthenticateSchemeAsync())?.Name);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/live")).StatusCode);
    }

    [Theory]
    [InlineData(false, 500)]
    [InlineData(true, 409)]
    public async Task Production_exception_middleware_returns_safe_problem_details(bool concurrency, int expectedStatus)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Production" });
        builder.WebHost.UseTestServer();
        builder.Services.AddProblemDetails();
        builder.Services.AddExceptionHandler<ApiExceptionHandler>();
        await using var app = builder.Build();
        app.UseExceptionHandler();
        app.Run(_ => throw (concurrency ? new DbUpdateConcurrencyException("private-test-detail") : new InvalidOperationException("private-test-detail")));
        await app.StartAsync();
        using var client = app.GetTestClient();
        var response = await client.GetAsync("/");
        Assert.Equal((HttpStatusCode)expectedStatus, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("private-test-detail", body);
        Assert.DoesNotContain("stack", body, StringComparison.OrdinalIgnoreCase);
        var problem = JsonDocument.Parse(body);
        Assert.Equal(expectedStatus, problem.RootElement.GetProperty("status").GetInt32());
        Assert.False(string.IsNullOrWhiteSpace(problem.RootElement.GetProperty("traceId").GetString()));
    }
}
