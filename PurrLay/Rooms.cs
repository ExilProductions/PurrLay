﻿namespace PurrLay;

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
    static readonly object _roomsLock = new();
    
    static ulong _roomIdCounter;
    
    public static async Task<string> CreateRoom(string region, string name)
    {
        lock (_roomsLock)
        {
            if (_room.ContainsKey(name))
                throw new Exception("Room already exists");
        }
        
        var hostSecret = Guid.NewGuid().ToString().Replace("-", "");

        await HTTPRestAPI.RegisterRoom(region, name);
        
        lock (_roomsLock)
        {
            var newRoomId = _roomIdCounter++;
            _roomIdToName.Add(newRoomId, name);
            _room.Add(name, new Room
            {
                name = name,
                hostSecret = hostSecret,
                clientSecret = Guid.NewGuid().ToString().Replace("-", ""),
                createdAt = DateTime.UtcNow,
                roomId = newRoomId
            });
        }
        
        return hostSecret;
    }

    public static bool TryGetRoom(string name, out Room? room)
    {
        lock (_roomsLock)
        {
            return _room.TryGetValue(name, out room);
        }
    }
    
    public static bool TryGetRoom(ulong roomId, out Room? room)
    {
        lock (_roomsLock)
        {
            if (_roomIdToName.TryGetValue(roomId, out var name) && _room.TryGetValue(name, out var r))
            {
                room = r;
                return true;
            }
        }

        room = null;
        return false;
    }

    public static async Task RemoveRoom(ulong roomId)
    {
        string? name;
        lock (_roomsLock)
        {
            if (!_roomIdToName.Remove(roomId, out name))
                return;
            _room.Remove(name);
        }
        await UnregisterWithRetry(name!);
    }

    static async Task UnregisterWithRetry(string name, int maxAttempts = 3, CancellationToken cancellationToken = default)
    {
        for (var attempt = 1; attempt <= maxAttempts && !cancellationToken.IsCancellationRequested; attempt++)
        {
            try
            {
                await HTTPRestAPI.UnregisterRoom(name);
                return;
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"UnregisterRoom attempt {attempt} failed for '{name}': {e.Message}");
                if (attempt == maxAttempts)
                {
                    Console.Error.WriteLine($"UnregisterRoom giving up for '{name}' after {attempt} attempts.");
                    return;
                }
                var delayMs = Math.Min(5000, 250 * (1 << Math.Min(attempt - 1, 5)));
                await Task.Delay(TimeSpan.FromMilliseconds(delayMs), cancellationToken);
            }
        }
    }
}