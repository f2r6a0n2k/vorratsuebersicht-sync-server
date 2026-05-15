namespace Vorratsuebersicht.SyncServer.Models;

public class SyncChangeLog
{
    public int SyncChangeLogId { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public int EntityId { get; set; }
    public string Operation { get; set; } = string.Empty;
    public string Timestamp { get; set; } = DateTime.UtcNow.ToString("O");
    public string? Data { get; set; }
}

public class ClientChange
{
    public string ClientChangeId { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public int? EntityId { get; set; }
    public string Operation { get; set; } = string.Empty;
    public Dictionary<string, object?>? Data { get; set; }
    public string? ClientTimestamp { get; set; }
}

public class PushResult
{
    public string ClientChangeId { get; set; } = string.Empty;
    public bool Accepted { get; set; }
    public int? EntityId { get; set; }
    public string? ConflictMessage { get; set; }
}

public class SyncChangeDto
{
    public int SyncChangeLogId { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public int EntityId { get; set; }
    public string Operation { get; set; } = string.Empty;
    public string Timestamp { get; set; } = string.Empty;
    public Dictionary<string, object?>? Data { get; set; }
}
