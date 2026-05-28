using SimPle.Domain.Common;

namespace SimPle.Domain.Hardware;

// ── Future: Hardware-enabled game support ─────────────────────────────────────
// Devices connect via:
//   1. Direct Wi-Fi (HTTP/WebSocket to backend)
//   2. Browser bridge (Web Serial / Web Bluetooth → frontend → backend)
//   3. MQTT/IoT bridge (device → broker → backend consumer)
//
// Uncomment and expand these stubs when adding embedded integration in a future phase.
// ─────────────────────────────────────────────────────────────────────────────

public class HardwareDevice : Entity
{
    public Guid OwnerId { get; private set; }
    public string DeviceKey { get; private set; } = default!;  // public identifier
    public string SecretHash { get; private set; } = default!; // hashed device secret
    public string FriendlyName { get; private set; } = default!;
    public string? GameSlug { get; private set; }              // which game this device is for
    public DeviceConnectionMode ConnectionMode { get; private set; }
    public DeviceStatus Status { get; private set; } = DeviceStatus.Offline;
    public DateTime? LastSeenAt { get; private set; }
    public string? PairingCode { get; private set; }
    public DateTime? PairingCodeExpiresAt { get; private set; }

    private HardwareDevice() { }

    public static HardwareDevice Register(Guid ownerId, string friendlyName, DeviceConnectionMode mode) => new()
    {
        OwnerId = ownerId,
        DeviceKey = Guid.NewGuid().ToString("N")[..12].ToUpper(),
        SecretHash = string.Empty, // Set by infrastructure after hashing
        FriendlyName = friendlyName,
        ConnectionMode = mode,
    };

    public void SetOnline() { Status = DeviceStatus.Online; LastSeenAt = DateTime.UtcNow; Touch(); }
    public void SetOffline() { Status = DeviceStatus.Offline; Touch(); }
    public void IssuePairingCode(string code, int ttlMinutes = 10) {
        PairingCode = code;
        PairingCodeExpiresAt = DateTime.UtcNow.AddMinutes(ttlMinutes);
        Touch();
    }
}

/// <summary>
/// Raw event from a hardware device (button press, sensor reading, etc.)
/// </summary>
public class DeviceInputEvent : Entity
{
    public Guid DeviceId { get; private set; }
    public Guid? SessionId { get; private set; }
    public string EventType { get; private set; } = default!;
    public string Payload { get; private set; } = "{}"; // JSON

    private DeviceInputEvent() { }

    public static DeviceInputEvent Create(Guid deviceId, string eventType, string payload, Guid? sessionId = null) => new()
    {
        DeviceId = deviceId,
        EventType = eventType,
        Payload = payload,
        SessionId = sessionId,
    };
}

public enum DeviceConnectionMode { DirectWifi, BrowserBridge, MqttBridge }
public enum DeviceStatus { Offline, Online, Pairing, InSession }
