using System.Text.Json.Serialization;

namespace SPTarkov.Core.Configuration;

public record Server
{
    public const string LocalServerId = "1721162719";

    public string Name { get; set; } = string.Empty;

    public string IpAddress { get; set; } = string.Empty;

    public string ServerId { get; set; } = string.Empty;

    [JsonIgnore]
    public bool Locked
    {
        // The built-in server is the only locked one.
        get { return ServerId == LocalServerId; }
    }

    public static Server Local
    {
        get
        {
            // The built-in server is locked and can not be edited; owned here and rebuilt every load.
            return new Server
            {
                Name = "Local Server",
                IpAddress = "127.0.0.1:6969",
                ServerId = LocalServerId,
            };
        }
    }
}
