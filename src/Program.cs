using System.Threading.RateLimiting;
using HotChocolate.AspNetCore;
using Microsoft.AspNetCore.RateLimiting;
using Planara.Common.Configuration;
using Planara.Common.GraphQL.Fusion;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.AddSettingsJson();

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 1024 * 1024 * 100;
});

builder.Services.AddRateLimiter(options =>
{
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await context.HttpContext.Response.WriteAsync("Rate limit exceeded", cancellationToken);
    };

    options.AddFixedWindowLimiter("graphql", limiter =>
    {
        limiter.PermitLimit = 100;
        limiter.Window = TimeSpan.FromSeconds(10);
        limiter.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiter.QueueLimit = 20;
    });

    options.AddFixedWindowLimiter("rest", limiter =>
    {
        limiter.PermitLimit = 50;
        limiter.Window = TimeSpan.FromSeconds(10);
        limiter.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiter.QueueLimit = 20;
    });
});

builder.Services.AddHealthChecks();

builder
    .Services
    .AddCors()
    .AddHttpClient("Fusion")
    .ConfigurePrimaryHttpMessageHandler(sp =>
    {
        var c = sp.GetRequiredService<IConfiguration>().GetValue<int?>("Fusion:MaxConnectionPerServer") ?? 10000;
        return new SocketsHttpHandler
        {
            UseProxy = false,
            UseCookies = false,
            Expect100ContinueTimeout = TimeSpan.Zero,
            MaxConnectionsPerServer = c
        };
    })
    .AddHeaderPropagation();

builder.Services.AddHeaderPropagation(c =>
{
    c.Headers.Add("GraphQL-Preflight");
    c.Headers.Add("Authorization");
});

builder
    .Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("Rest"));

builder
    .Services
    .AddFusionGatewayServer()
    .UseSchemaFromRedis(
        _ =>
            ConnectionMultiplexer.Connect(
                builder.Configuration.GetValue<string>("DbConnections:Redis:ConnectionString")!,
                c => c.CertificateValidation += (_, _, _, _) => true),
        builder.Configuration.GetSection("GraphQL")
    );

var app = builder.Build();

app.UseRouting();

app.UseCors(c => c
    .AllowAnyOrigin()
    .AllowAnyHeader()
    .AllowAnyMethod()
);

app.UseRateLimiter();

app.UseHeaderPropagation();

app.MapHealthChecks("/health");

app.MapGraphQL("/api")
    .RequireRateLimiting("graphql")
    .WithOptions(new GraphQLServerOptions
    {
        Tool = { Enable = app.Environment.IsDevelopment() }
    });

app.MapReverseProxy();

app.Run();
