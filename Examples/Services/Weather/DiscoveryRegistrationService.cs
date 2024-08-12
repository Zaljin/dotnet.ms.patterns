using System.Text.Json;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Net.Sockets;
using Microsoft.AspNetCore.Http.Extensions;

namespace Weather;

public static class CommonExtensions
{
    public static IHostApplicationBuilder AddAPIDiscovery(this IHostApplicationBuilder builder, Action<ICommonAPIBuilder>? configure = null)
    {
        var commonBuilder = new CommonAPIBuilder(builder);
        configure?.Invoke(commonBuilder);
        return builder;
    }
}

public interface ICommonAPIBuilder
{
    CommonAPIBuilder AddCrudAPI<TContract, TModel>() where TContract : APIContract, new();
    CommonAPIBuilder MakeAPIDiscoverable(string name, string[] versions);
}

public class CommonAPIBuilder : ICommonAPIBuilder
{
    private readonly IHostApplicationBuilder _hostBuilder;
    private IServiceCollection Services => _hostBuilder.Services;

    internal CommonAPIBuilder(IHostApplicationBuilder hostBuilder)
    {
        _hostBuilder = hostBuilder;
        AddDiscoveryConfigurations();
        AddInternalServices();
    }

    public CommonAPIBuilder AddCrudAPI<TContract, TModel>()
        where TContract : APIContract, new()
    {
        AddHttpClientForContract(new TContract());
        Services.AddSingleton<ICrudAPIService<TModel>, CrudAPIService<TModel, TContract>>();
        return this;
    }

    public CommonAPIBuilder MakeAPIDiscoverable(string name, string[] versions)
    {
        AddRegistrationConfigurations(name, versions);
        Services.AddHostedService<DiscoveryRegistrationService>();
        return this;
    }

    private void AddHttpClientForContract<TContract>(TContract contract)
        where TContract : APIContract
    {
        Services.AddOptions<APIDiscoveryConfiguration>()
            .Configure((APIDiscoveryConfiguration discovery, IConfiguration config) =>
            {
                discovery.Contracts.TryAdd($"{contract.Name}-{contract.Version}", contract);
            });
        Services.AddHttpClient(contract.Name)
            .ConfigureHttpClient(async (provider, client) =>
            {
                var discoveryService = provider.GetRequiredService<IAPIDiscoveryService>();
                var model = await discoveryService.GetServiceUrl(contract.Name, contract.Version);

                if (model == null)
                {
                    throw new Exception("Could not locate service.");
                }

                client.BaseAddress = new Uri(model.Endpoint);
            });
    }

    private void AddDiscoveryConfigurations()
    {
        Services.AddOptionsWithValidateOnStart<APIDiscoveryConfiguration>()
            .Configure((APIDiscoveryConfiguration discovery, IConfiguration config) =>
            {
                var serviceUrl = config.GetValue<string>("ApiDiscovery:ServiceUrl");

                if (serviceUrl == null)
                {
                    throw new ArgumentNullException("'ServiceUrl' env var must be set.");
                }

                discovery.ServiceUrl = serviceUrl;
            });
    }

    private void AddRegistrationConfigurations(string name, string[] versions)
    {
        Services.AddOptionsWithValidateOnStart<APIDiscoveryConfiguration>()
            .Configure((APIDiscoveryConfiguration discovery, IConfiguration config) =>
            {
                var selfUrl = config.GetValue<string>("ApiDiscovery:SelfUrl");

                if (selfUrl == null)
                {
                    throw new ArgumentNullException("'SelfUrl' env var must be set.");
                }

                discovery.Name = name;
                discovery.SupportedVersions = versions;
                discovery.SelfUrl = selfUrl;
            });
    }

    private void AddInternalServices()
    {
        Services.AddHttpClient("Discovery")
            .ConfigureHttpClient((provider, client) =>
            {
                var options = provider
                    .GetRequiredService<IOptions<APIDiscoveryConfiguration>>()
                    .Value;

                client.BaseAddress = new Uri(options.ServiceUrl);
            });
        Services.AddSingleton<IAPIDiscoveryService, APIDiscoveryService>();
    }
}

public interface ICrudAPIService<TModel>
{
    Task<TModel?> GetById(string id);
}

public class CrudAPIService<TModel, TContract> : ICrudAPIService<TModel> where TContract : APIContract
{
    protected TContract _contract;
    private readonly HttpClient _client;

    public CrudAPIService(TContract contract, IHttpClientFactory clientFactory)
    {
        _contract = contract;
        _client = clientFactory.CreateClient($"{contract.Name}-{contract.Version}");
    }

    public async Task<TModel?> GetById(string id)
    {
        var result = await _client.GetAsync($"/{id}");
        var model = await result.Content.ReadFromJsonAsync<TModel>();
        return model;
    }
}

public abstract class APIContract(string name, string version)
{
    public string Name => name;
    public string Version => version;
}

public class APIDiscoveryConfiguration
{
    public string Name { get; set; } = string.Empty;
    public string[] SupportedVersions { get; set; } = [];
    public string SelfUrl { get; set; } = string.Empty;
    public string ServiceUrl { get; set; } = string.Empty;
    public Dictionary<string, APIContract> Contracts { get; } = [];
}

public class DiscoveryAPIModel
{
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
}

public interface IAPIDiscoveryService
{
    Task<DiscoveryAPIModel?> GetServiceUrl(string name, string version);
    Task RegisterSelf();
}

public class APIDiscoveryService(IHttpClientFactory clientFactory, IOptions<APIDiscoveryConfiguration> options) : IAPIDiscoveryService
{
    private APIDiscoveryConfiguration Configuration => options.Value;

    public async Task<DiscoveryAPIModel?> GetServiceUrl(string name, string version)
    {
        using var client = clientFactory.CreateClient("Discovery");
        var queryHelper = new QueryBuilder
        {
            { "name", name },
            { "version", version }
        };
        var url = "Discovery" + queryHelper.ToQueryString();
        var result = await client.GetAsync(url);
        var model = await result.Content.ReadFromJsonAsync<DiscoveryAPIModel>();

        return model;
    }

    public async Task RegisterSelf()
    {

        var models = Configuration.SupportedVersions
            .Select(version => new DiscoveryAPIModel()
            {
                Name = Configuration.Name,
                Version = version,
                Endpoint = Configuration.SelfUrl
            })
            .Select(model => new StringContent(
                JsonSerializer.Serialize(model),
                Encoding.UTF8,
                "application/json")
            );

        using var client = clientFactory.CreateClient("Discovery");
        foreach (var item in models)
        {
            await client.PutAsync("Discovery", item);
            item.Dispose();
        }
    }
}

public class DiscoveryRegistrationService(IAPIDiscoveryService service) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await service.RegisterSelf();
            await Task.Delay(60000, stoppingToken);
        }
    }
}