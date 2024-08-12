using Discovery;
using Discovery.Core.Exceptions;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddSingleton<IDiscoveryRepository, DiscoveryRepository>();
builder.Services.AddSingleton<IDiscoveryService, DiscoveryService>();

var app = builder.Build();

// Configure the HTTP request pipeline.

var exceptionMap = new Dictionary<string, int>
{
    { nameof(Exception), 500 },
    { nameof(ArgumentException), 400 },
    { nameof(ArgumentNullException), 400 },
    { nameof(DiscoveryNotRegisteredException), 404 }
};

app.UseExceptionHandler(exceptionHandlerApp =>
{
    exceptionHandlerApp.Run(async context =>
    {
        var feature = context.Features.Get<IExceptionHandlerFeature>();
        var error = feature?.Error;

        if (error != null)
        {
            var errorName = error.GetType().Name;
            var errorDescription = "Internal error";
            var traceId = context.TraceIdentifier;

            if (exceptionMap.TryGetValue(errorName, out int code))
            {
                errorDescription = error.Message;
            }
            else
            {
                code = StatusCodes.Status500InternalServerError;
            }

            var response = new
            {
                Description = errorDescription,
                TraceId = traceId,
            };

            context.Response.Headers.TraceParent = traceId;
            context.Response.StatusCode = code;
            await context.Response.WriteAsJsonAsync(response);
        }

        await Task.CompletedTask;
    });
});

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
