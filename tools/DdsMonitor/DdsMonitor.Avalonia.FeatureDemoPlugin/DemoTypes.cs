using CycloneDDS.Schema;

namespace DdsMonitor.Avalonia.FeatureDemoPlugin;

// ── Telemetry — unkeyed, fast (10 Hz) ────────────────────────────────────────

[DdsTopic("FeatureDemo/Telemetry")]
public struct TelemetrySample
{
    public long Timestamp;
    public int SequenceId;
    public double Cpu;
    public double Memory;
    public float Temperature;
}

// ── Entity state — keyed (5 Hz) ───────────────────────────────────────────────

[DdsTopic("FeatureDemo/EntityState")]
public struct EntityState
{
    [DdsKey]
    public int EntityId;
    public string Name;
    public EntityKind Kind;
    public float X;
    public float Y;
    public float Z;
    public byte Health;
    public bool IsAlive;
}

public enum EntityKind { Player, Npc, Vehicle, Projectile }

// ── Alert — unkeyed, slow ─────────────────────────────────────────────────────

[DdsTopic("FeatureDemo/Alert")]
public struct AlertEvent
{
    public long Timestamp;
    public Severity Level;
    public string Message;
    public string Origin;
}

public enum Severity { Info, Warning, Error, Critical }

// ── Geo location — unkeyed, nested struct ─────────────────────────────────────

[DdsTopic("FeatureDemo/GeoLocation")]
public struct GeoLocation
{
    public double Latitude;
    public double Longitude;
    public float Altitude;
    public Address NestedAddress;
}

public struct Address
{
    public string Street;
    public string City;
    public string Country;
}

// ── Union payload — demonstrates DDS union ────────────────────────────────────

[DdsTopic("FeatureDemo/UnionPayload")]
[DdsUnion]
public struct UnionPayload
{
    [DdsDiscriminator]
    public int Discriminator;

    [DdsCase(1)]
    public int IntValue;

    [DdsCase(2)]
    public string StringValue;

    [DdsCase(3)]
    public double DoubleValue;

    [DdsDefaultCase]
    public bool DefaultValue;
}
