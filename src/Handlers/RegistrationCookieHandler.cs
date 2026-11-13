namespace Planara.Gateway.Handlers;

public class RegistrationCookieHandler(
    IHttpContextAccessor contextAccessor, IConfiguration configuration) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var context = contextAccessor.HttpContext;
        var requestUri = request.RequestUri;

        if (context?.Request.Headers["X-Planara-Registration-Flow"].ToString() != "1" ||
            requestUri is null ||
            !Uri.TryCreate(configuration["GraphQL:Auth:Uri"], UriKind.Absolute, out var authUri) ||
            requestUri.Authority != authUri.Authority)
            return await base.SendAsync(request, cancellationToken);

        var jwt = context.Request.Cookies["__Host-planara-registration"];

        if (!string.IsNullOrWhiteSpace(jwt))
            request.Headers.TryAddWithoutValidation("Cookie", $"__Host-planara-registration={jwt}");

        var response = await base.SendAsync(request, cancellationToken);

        if (!response.Headers.TryGetValues("Set-Cookie", out var cookies))
            return response;

        foreach (var cookie in cookies)
            if (cookie.StartsWith("__Host-planara-registration=", StringComparison.Ordinal))
                context.Response.Headers.Append("Set-Cookie", cookie);

        return response;
    }
}