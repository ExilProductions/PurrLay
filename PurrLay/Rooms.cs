namespace PurrLay;

public class Room
{
    public string? name;
    public string? hostSecret;
    public string? clientSecret;
    public DateTime createdAt;
    public ulong roomId;
}

public static class Lobby
{
    static readonly Dictionary<string, Room> _room = new();
    static readonly Dictionary<ulong, string> _roomIdToName = new();
    
    static ulong _roomIdCounter;
    
    public static async Task<string> CreateRoom(string region, string name)
    {
        if (_room.ContainsKey(name))
            throw new Exception("Room already exists");
        
        var hostSecret = Guid.NewGuid().ToString().Replace("-", "");

        await HTTPRestAPI.RegisterRoom(region, name);
        
        _roomIdToName.Add(_roomIdCounter, name);
        _room.Add(name, new Room
        {
            name = name,
            hostSecret = hostSecret,
            clientSecret = Guid.NewGuid().ToString().Replace("-", ""),
            createdAt = DateTime.UtcNow,
            roomId = _roomIdCounter++
        });
        
        return hostSecret;
    }

    public static bool TryGetRoom(string name, out Room? room)
    {
        return _room.TryGetValue(name, out room);
    }
    
    public static bool TryGetRoom(ulong roomId, out Room? room)
    {
        room = null;
        return _roomIdToName.TryGetValue(roomId, out var name) && _room.TryGetValue(name, out room);
    }

    public static void RemoveRoom(ulong roomId)
    {
        if (_roomIdToName.Remove(roomId, out var name))
        {
            _room.Remove(name);
            _ = HTTPRestAPI.UnregisterRoom(name);
        }
    }
}