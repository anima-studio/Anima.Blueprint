using Aspire.Hosting;

using Microsoft.Extensions.Configuration;

using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

using AppHost = Moser.Solutions.Blueprint.AppHost;

[assembly: CollectionBehavior(DisableTestParallelization = false)]

namespace Moser.Solutions.Blueprint.Tests;

[Collection("DistributedAppTestCollection")]
public class DistributedAppTestFixture : IAsyncLifetime
{
    private DistributedApplication? _distributedApp { get; set; }
    public IResourceCollection? Resources { get; private set; }
    public IConfiguration? Configuration { get; private set; }
    public ResourceNotificationService? ResourceNotificationService { get; private set; }
    public HttpClient? HttpClient { get; private set; }
    public JsonSerializerOptions? JsonSerializerOptions { get; private set; }

    public async Task InitializeAsync()
    {
        var distributedApplicationTestingBuilder = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.AppHost>();

        //Resources = distributedApplicationTestingBuilder.Resources
        //    .Remove(r => r.Name == AppHost.Program.DeviceRegistryApiName)
        //    .Remove(r => r.Name == AppHost.Program.CommunicationServiceApiName);

        _distributedApp = await distributedApplicationTestingBuilder.BuildAsync();

        ResourceNotificationService = _distributedApp.Services.GetRequiredService<ResourceNotificationService>();

        Configuration = _distributedApp.Services.GetRequiredService<IConfiguration>();

        await _distributedApp.StartAsync();

        await ResourceNotificationService
            .WaitForResourceAsync(AppHost.Program.UsersRegistryApiName, KnownResourceStates.Running)
            .WaitAsync(TimeSpan.FromSeconds(30));

        HttpClient = BuildCertBypassHttpClient();

        JsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        //?Client = new ?ApiClient(this);
    }

    private HttpClient BuildCertBypassHttpClient()
    {
        var baseAddress = _distributedApp?.GetEndpoint(AppHost.Program.UsersRegistryApiName, "https");

        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };

        return new HttpClient(handler)
        {
            BaseAddress = baseAddress,
            Timeout = TimeSpan.FromMinutes(3)
        };
    }

    private HttpClient BuildDistributedAppHttpClient()
    {
        var httpClient = _distributedApp?.CreateHttpClient(AppHost.Program.UsersRegistryApiName);
        httpClient!.Timeout = TimeSpan.FromMinutes(3);
        return httpClient;
    }

    public async Task DisposeAsync()
    {
        HttpClient?.Dispose();

        if (_distributedApp is not null)
        {
            await _distributedApp.StopAsync();
            await _distributedApp.DisposeAsync();
        }
    }

    [Fact]
    public async Task GetWebResourceRootReturnsOkStatusCode()
    {
        // Arrange
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.AppHost>();
        appHost.Services.ConfigureHttpClientDefaults(clientBuilder =>
        {
            clientBuilder.AddStandardResilienceHandler();
        });
        // To output logs to the xUnit.net ITestOutputHelper, consider adding a package from https://www.nuget.org/packages?q=xunit+logging

        await using var app = await appHost.BuildAsync();
        var resourceNotificationService = app.Services.GetRequiredService<ResourceNotificationService>();
        await app.StartAsync();

        // Act
        using var client = CreateTestHttpClient();
        //var httpClient = app.CreateHttpClient("webfrontend");
        await resourceNotificationService.WaitForResourceAsync("webfrontend", KnownResourceStates.Running).WaitAsync(TimeSpan.FromSeconds(30));
        //var response = await httpClient.GetAsync("/");
        var response = await client.GetAsync("/");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private HttpClient CreateTestHttpClient()
    {
        var handler = new HttpClientHandler();

        handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
        
        return new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(15)
        };
    }
}
