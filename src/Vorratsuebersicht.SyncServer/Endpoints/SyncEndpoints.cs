using Vorratsuebersicht.SyncServer.Models;
using Vorratsuebersicht.SyncServer.Services;

namespace Vorratsuebersicht.SyncServer.Endpoints;

public static class SyncEndpoints
{
    public static WebApplication MapSyncEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/sync");

        group.MapGet("/changes", (SyncService syncService, string? since) =>
        {
            DateTime? sinceDate = null;
            if (!string.IsNullOrWhiteSpace(since) && DateTime.TryParse(since, out var parsed))
            {
                sinceDate = parsed.ToUniversalTime();
            }

            var changes = syncService.PullChanges(sinceDate);
            return Results.Ok(changes);
        })
        .WithName("SyncPull")
        ;

        group.MapPost("/push", (SyncService syncService, List<ClientChange> changes) =>
        {
            if (changes == null || changes.Count == 0)
                return Results.Ok(new List<PushResult>());

            var results = syncService.PushChanges(changes);
            return Results.Ok(results);
        })
        .WithName("SyncPush")
        ;

        return app;
    }
}
