using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Planara.Common.Auth.Jwt;
using Planara.Common.Configuration;
using Planara.Common.GraphQL.Fusion;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.AddSettingsJson();

builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("graphql", limiter =>
    {
        limiter.PermitLimit = 100;
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
    });

builder.Services.AddHeaderPropagation(c =>
{
    c.Headers.Add("GraphQL-Preflight");
    c.Headers.Add("Authorization");
});

builder
    .Services
    .AddJwtAuth(builder.Configuration)
    .AddFusionGatewayServer()
    .UseSchemaFromRedis(
        _ =>
            ConnectionMultiplexer.Connect(
                builder.Configuration.GetValue<string>("DbConnections:Redis:ConnectionString")!,
                c => c.CertificateValidation += (_, _, _, _) => true),
        builder.Configuration.GetSection("GraphQL")
    );

var app = builder.Build();

app.UseRateLimiter();

app.MapHealthChecks("/health");

app
    .UseCors(c => c.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod())
    .UseWebSockets()
    .UseRouting()
    .UseAuthentication()
    .UseAuthorization()
    .UseHeaderPropagation() ;

app.MapGraphQL("/planara/api")
    .RequireRateLimiting("graphql");

app.Run();
