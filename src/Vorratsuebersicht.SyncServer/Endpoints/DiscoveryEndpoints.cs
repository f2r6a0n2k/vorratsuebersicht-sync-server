using System.Runtime.InteropServices;
using System.Net;
using System.Net.Sockets;

namespace Vorratsuebersicht.SyncServer.Endpoints;

public static class DiscoveryEndpoints
{
    public static WebApplication MapDiscoveryEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/discovery");

        group.MapGet("/", (IConfiguration config) =>
        {
            var serverName = config.GetValue<string>("Server:Name") ?? "Vorratsuebersicht SyncServer";
            var hostName = Dns.GetHostName();
            var localIps = GetLocalIPAddresses();

            return Results.Ok(new
            {
                Name = serverName,
                Version = "1.0.0",
                HostName = hostName,
                LocalIPs = localIps,
                Framework = RuntimeInformation.FrameworkDescription,
                OS = RuntimeInformation.OSDescription,
                OSArchitecture = RuntimeInformation.OSArchitecture.ToString(),
                Endpoints = new
                {
                    Articles = "/api/articles",
                    StorageItems = "/api/storage-items",
                    ShoppingItems = "/api/shopping-items",
                    SyncPull = "/api/sync/changes?since={timestamp}",
                    SyncPush = "/api/sync/push",
                    WebUI = "/"
                }
            });
        })
        .WithName("Discovery")
        ;

        group.MapGet("/ping", () => Results.Ok(new { Status = "ok", Timestamp = DateTime.UtcNow.ToString("O") }))
            .WithName("Ping")
            ;

        return app;
    }

    private static List<string> GetLocalIPAddresses()
    {
        var ips = new List<string>();
        try
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    ips.Add(ip.ToString());
                }
            }
        }
        catch
        {
            // Network not available
        }
        return ips;
    }
}
