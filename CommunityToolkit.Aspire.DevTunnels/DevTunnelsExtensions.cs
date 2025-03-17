using Azure.Core;
using Azure.Identity;
using Microsoft.DevTunnels.Connections;
using Microsoft.DevTunnels.Contracts;
using Microsoft.DevTunnels.Management;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;

namespace CommunityToolkit.Aspire.DevTunnels;

public static class DevTunnelsExtensions
{
    // TODO: this right now is very much tailored towards how I want to use it for .NET MAUI and is hacked together,
    // but its a start of how a Community Toolkit integration could look like
    public async static Task<TBuilder> AddDevTunnels<TBuilder>(this TBuilder builder) where TBuilder : IDistributedApplicationBuilder
    {
        // Create a tunnel management client
        var tunnelManagementClient = new TunnelManagementClient(new ProductInfoHeaderValue("Aspire.DevTunnels", "v0.1"), async () =>
        {
            // TODO: this assumes you have Dev Tunnels enabled and are logged in
            var credential = new DefaultAzureCredential();
            var token = await credential.GetTokenAsync(new TokenRequestContext(
                [$"{TunnelServiceProperties.Production.ServiceAppId}/.default"]));

            return new AuthenticationHeaderValue("Bearer", token.Token);
        }, ManagementApiVersions.Version20230927Preview);

        // Create a new dev tunnel
        var tunnel = new Tunnel
        {
            AccessControl = new TunnelAccessControl
            {
                Entries =
                [
                    new TunnelAccessControlEntry
                    {
                        Type = TunnelAccessControlEntryType.Anonymous,
                        Scopes = [TunnelAccessScopes.Connect],
                    }
                ],
            },
        };

        List<TunnelPort> portsToOpen = [];
        var devTunnelsResource = new DevTunnelsResource();

        // Collect all the ports from the builder's resources
        foreach (var resource in builder.Resources)
        {
            if (resource is IResourceWithEnvironment envResource)
            {
                // TODO: probably also check if this is a localhost endpoint? Otherwise don't bother?
                foreach (var endpoint in envResource.Annotations
                    .Where(a => a is EndpointAnnotation annotation && annotation.Port is not null)
                    .Cast<EndpointAnnotation>())
                {
                    portsToOpen.Add(new TunnelPort
                    {
                        Name = endpoint.Name,
                        PortNumber = (ushort)endpoint.Port!,
                        Protocol = endpoint.UriScheme.ToString().ToLowerInvariant()
                    });
                }
            }
        }

        // Collect all the ports from the environment variables that we want to tunnel as well
        var variablesToInclude = new HashSet<string>
        {
            "DOTNET_DASHBOARD_OTLP_ENDPOINT_URL",
            "DOTNET_DASHBOARD_URL",
            "DOTNET_RESOURCE_SERVICE_ENDPOINT_URL",
        };

        foreach (string variable in variablesToInclude)
        {
            if (!string.IsNullOrEmpty(builder.Configuration[variable]?.ToString()))
            {
                // There can be multiple values split by a semicolon so split and process each
                var value = builder.Configuration[variable]?.ToString();
                var values = value?.Split(';') ?? [];
                foreach (var val in values)
                {
                    if (Uri.TryCreate(val, UriKind.Absolute, out var uri))
                    {
                        portsToOpen.Add(new TunnelPort
                        {
                            Name = variable,
                            PortNumber = (ushort)uri.Port,
                            Protocol = uri.Scheme.ToLowerInvariant()
                        });
                    }
                }
            }
        }

        tunnel.Ports = [.. portsToOpen];

        tunnel = await tunnelManagementClient.CreateOrUpdateTunnelAsync(tunnel, null, CancellationToken.None);

        Console.WriteLine($"Tunnel Created: {tunnel.TunnelId} for ports:{Environment.NewLine}" +
            $"{string.Join(Environment.NewLine, tunnel.Ports?.Select(p => p.PortNumber) ?? [])}");

        // Start the tunnel host
        var tunnelHost = new TunnelRelayTunnelHost(tunnelManagementClient, new TraceSource("foo"));
        await tunnelHost.ConnectAsync(tunnel);

        Console.WriteLine($"Tunnel started at: {tunnel.Endpoints?[0]?.TunnelUri}");
        Environment.SetEnvironmentVariable("DEVTUNNEL_ID", GetTunnelId(tunnel.Endpoints[0].TunnelUri.ToString()));

        devTunnelsResource.Annotations.Add(new ResourceSnapshotAnnotation(new CustomResourceSnapshot()
        {
            ResourceType = "DevTunnelsResource",
            Properties = [],
            Urls = [.. tunnel.Ports.Select(p => new UrlSnapshot(p.PortNumber.ToString(), p.PortForwardingUris[0], false))],
            StartTimeStamp = DateTime.UtcNow,
            State = new ResourceStateSnapshot(KnownResourceStates.Running, KnownResourceStateStyles.Success),
        }));

        builder.AddResource(devTunnelsResource);

        return builder;
    }

    private static string GetTunnelId(string uri)
    {
        string pattern = @"https://([a-zA-Z0-9]+)\.([a-zA-Z0-9]+)\.devtunnels\.ms/";

        Match match = Regex.Match(uri, pattern);
        if (match.Success)
        {
            string extractedPart = match.Groups[1].Value;
            return extractedPart;
        }

        return string.Empty;
    }
}
