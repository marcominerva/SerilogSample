using System.Diagnostics;
using Serilog;
using Serilog.Core;
using Serilog.Debugging;
using Serilog.Events;

Environment.SetEnvironmentVariable("BASE_DIRECTORY", Environment.GetEnvironmentVariable("HOME") ?? AppContext.BaseDirectory);

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog((hostingContext, services, loggerConfiguration) =>
{
    loggerConfiguration.ReadFrom.Configuration(hostingContext.Configuration);
    loggerConfiguration.ReadFrom.Services(services);

    SelfLog.Enable(message =>
    {
        Debug.Print(message);
    });
});

// Add services to the container.
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<ILogEventEnricher, HttpContextEnricher>();

builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseHttpsRedirection();

app.MapOpenApi();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/openapi/v1.json", app.Environment.ApplicationName);
});

app.UseRouting();

//app.UseAuthentication();

app.UseSerilogRequestLogging(options =>
{
    options.IncludeQueryInRequestPath = true;
});

//app.UseAuthorization();

app.MapGet("/api/ping", (ILogger<Program> logger) =>
{
    logger.LogInformation("Ping starting...");

    logger.LogError("Unknown error");
    logger.LogWarning("Ping completed with warnings.");

    return TypedResults.Ok();
});

app.Run();

public class HttpContextEnricher(IHttpContextAccessor httpContextAccessor) : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var httpContext = httpContextAccessor.HttpContext;

        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("LevelNumber", (int)logEvent.Level));
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("IsHttp", httpContext is not null));

        if (httpContext is null)
        {
            return;
        }

        var userName = "marco.minerva@gmail.com"; // httpContext.User.Identity?.Name;
        if (!string.IsNullOrWhiteSpace(userName))
        { 
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("UserName", userName));
        }
    }
}