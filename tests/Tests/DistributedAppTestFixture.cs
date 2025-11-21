using Aspire.Hosting;

using Microsoft.Extensions.Configuration;

using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

using AppHost = Anima.Blueprint.AppHost;

[assembly: CollectionBehavior(DisableTestParallelization = false)]

namespace Anima.Blueprint.Tests;

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
            .WaitForResourceAsync(AppHost.Program.CatalogApiName, KnownResourceStates.Running)
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
        var baseAddress = _distributedApp?.GetEndpoint(AppHost.Program.CatalogApiName, "https");

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
        var httpClient = _distributedApp?.CreateHttpClient(AppHost.Program.CatalogApiName);
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
}
