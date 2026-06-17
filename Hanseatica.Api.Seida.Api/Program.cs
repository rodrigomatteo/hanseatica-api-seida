using Hanseatica.Api.Seida.Api;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.AddCustomHealthChecks(builder.Configuration);

// Add OpenAPI documentation
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Info = new()
        {
            Title = "Hanseatica SEIDA API",
            Version = "v1",
            Description = "API for submitting files and querying submission status through the SEIDA service."
        };
        return Task.CompletedTask;
    });
});

// Add logging
builder.Services.AddLogging(config =>
{
    config.ClearProviders();
    config.AddConsole();
    config.AddDebug();
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("Hanseatica SEIDA API");
        options.WithDefaultOpenAllTags(true);
    });
}

// Health check endpoint
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = healthCheck => healthCheck.Tags.Contains("ready")
});

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
