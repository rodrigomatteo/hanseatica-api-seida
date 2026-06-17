namespace Hanseatica.Api.Seida.Api;

using Application.Interfaces;
using Application.Services;
using Infrastructure.ExternalServices;
using Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Refit;

/// <summary>
/// Extension methods for configuring dependency injection
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Application Services
        services.AddScoped<ISubmissionService, SubmissionService>();
        services.AddScoped<IQueryService, QueryService>();

        // Infrastructure Services
        services.AddScoped<IFileService, FileService>();
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<ISeidaClient, SeidaClient>();
        services.AddScoped<NetworkService>();

        // Refit HTTP Client
        var seidaBaseUrl = configuration["Seida:BaseUrl"] ?? configuration["ExternalServices:SeidaBaseUrl"]
            ?? throw new InvalidOperationException("SEIDA service base URL is not configured");

        var timeoutSeconds = int.TryParse(configuration["Seida:TimeoutSeconds"], out var t) ? t : 60;

        services.AddRefitClient<ISeidaHttpClient>()
            .ConfigureHttpClient(client =>
            {
                client.BaseAddress = new Uri(seidaBaseUrl);
                client.Timeout = TimeSpan.FromSeconds(timeoutSeconds + 5);
            })
            .AddStandardResilienceHandler(options =>
            {
                options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
                options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
                options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(timeoutSeconds * 2);
            });

        return services;
    }

    public static IServiceCollection AddCustomHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        var healthChecks = services.AddHealthChecks();

        // System health checks
        healthChecks.AddDiskStorageHealthCheck(options =>
            options.AddDrive(Path.GetPathRoot(AppContext.BaseDirectory)));

        // External service health check
        var seidaUrl = configuration["Seida:BaseUrl"] ?? configuration["ExternalServices:SeidaBaseUrl"];
        if (!string.IsNullOrEmpty(seidaUrl))
        {
            healthChecks.AddUrlGroup(new Uri(seidaUrl), name: "seida-service", tags: ["external"]);
        }

        return services;
    }
}
