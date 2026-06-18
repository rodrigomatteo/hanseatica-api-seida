using Hanseatica.Api.Seida.Api;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.AddCustomHealthChecks(builder.Configuration);

// Add Swagger documentation
builder.Services.AddSwaggerGen();

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
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Hanseatica SEIDA API v1");
        options.RoutePrefix = "swagger";
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
