using System.Net;
using System.Net.Sockets;
using Vorratsuebersicht.SyncServer.Data;
using Vorratsuebersicht.SyncServer.Endpoints;
using Vorratsuebersicht.SyncServer.Services;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory
});

var dbPath = builder.Configuration.GetValue<string>("Server:DatabasePath") ?? "vorratsuebersicht.db";
var connectionString = $"Data Source={dbPath}";

builder.Services.AddSingleton(new SyncDbContext(connectionString));
builder.Services.AddSingleton<SyncService>();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

app.UseCors();
app.UseStaticFiles();

app.MapArticleEndpoints();
app.MapStorageItemEndpoints();
app.MapShoppingItemEndpoints();
app.MapSyncEndpoints();
app.MapMigrateEndpoints();
app.MapDiscoveryEndpoints();

app.MapFallbackToFile("index.html");

var serverName = app.Configuration.GetValue<string>("Server:Name") ?? "Vorratsuebersicht SyncServer";

app.Lifetime.ApplicationStarted.Register(() =>
{
    Console.WriteLine();
    Console.WriteLine($"  {serverName}");
    Console.WriteLine($"  ====================");
    Console.WriteLine();
    Console.WriteLine($"  Datenbank: {Path.GetFullPath(dbPath)}");
    Console.WriteLine();
    Console.WriteLine($"  Verfügbar im LAN unter:");
    foreach (var ip in GetLocalIPv4Addresses())
    {
        var port = app.Urls.FirstOrDefault()?.Split(':').Last() ?? "5191";
        Console.WriteLine($"    http://{ip}:{port}/");
    }
    Console.WriteLine();
    Console.WriteLine($"  API-Endpunkte:");
    Console.WriteLine($"    Discovery:  GET  /api/discovery");
    Console.WriteLine($"    Ping:       GET  /api/discovery/ping");
    Console.WriteLine($"    Artikel:    GET/POST  /api/articles");
    Console.WriteLine($"                GET/PUT/DELETE  /api/articles/{{id}}");
    Console.WriteLine($"    Lager:      GET/POST  /api/storage-items");
    Console.WriteLine($"                GET/PUT/DELETE  /api/storage-items/{{id}}");
    Console.WriteLine($"    Einkauf:    GET/POST  /api/shopping-items");
    Console.WriteLine($"                GET/PUT/DELETE  /api/shopping-items/{{id}}");
    Console.WriteLine($"    Sync Pull:  GET  /api/sync/changes?since={{timestamp}}");
    Console.WriteLine($"    Sync Push:  POST /api/sync/push");
    Console.WriteLine();
});

app.Run();

static List<string> GetLocalIPv4Addresses()
{
    var ips = new List<string>();
    try
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork &&
                !IPAddress.IsLoopback(ip))
            {
                ips.Add(ip.ToString());
            }
        }
    }
    catch { }
    return ips;
}
