# Forwarded Headers Configuration for Azure App Service

## Why We Clear KnownProxies and KnownNetworks

When deploying ASP.NET Core applications to Azure Linux App Service, TLS is terminated by Azure's reverse proxy. The application must process `X-Forwarded-For` and `X-Forwarded-Proto` headers to:
- Get the correct client IP address
- Detect the original request scheme (HTTP/HTTPS)
- Generate correct OIDC/OAuth redirects

## Microsoft's Official Guidance

Per [Microsoft's official documentation](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/proxy-load-balancer?view=aspnetcore-10.0):

> "To forward the scheme from the proxy in non-IIS scenarios, enable the Forwarded Headers Middleware by setting `ASPNETCORE_FORWARDEDHEADERS_ENABLED` to `true`."
>
> **Warning:** This flag uses settings designed for cloud environments and **doesn't enable features such as the KnownProxies option to restrict which IPs forwarders are accepted from.**

## Why This Is Secure

Clearing `KnownProxies` and `KnownNetworks` is **intentional for Azure App Service**:
- Azure's infrastructure provides the security boundary
- The reverse proxy is within Azure's trusted network layer
- Restricting specific proxy IPs would break when Azure's internal infrastructure changes

## Implementation

Our code implements this pattern by clearing the default restrictions:

```csharp
var forwardedHeadersOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
};

forwardedHeadersOptions.KnownProxies.Clear();
forwardedHeadersOptions.KnownNetworks.Clear();
```

This is functionally equivalent to setting the `ASPNETCORE_FORWARDEDHEADERS_ENABLED` environment variable.

## References

- [Configure ASP.NET Core to work with proxy servers and load balancers - Microsoft Learn](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/proxy-load-balancer?view=aspnetcore-10.0)
- [Configure ASP.NET Core apps - Azure App Service](https://learn.microsoft.com/en-us/azure/app-service/configure-language-dotnetcore)
