using Microsoft.AspNetCore.Builder;
using Amazon.Extensions.Configuration.SystemsManager;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;

namespace Common;

public static class CommonExtensions
{
    public static IHostBuilder AddThings(this IHostBuilder builder, string name)
    {
        builder.ConfigureAppConfiguration((hostingContext, config) =>
        {
            config.AddSystemsManager($"/{name}/", TimeSpan.FromMinutes(1));
        });
        return builder;
    }
}
