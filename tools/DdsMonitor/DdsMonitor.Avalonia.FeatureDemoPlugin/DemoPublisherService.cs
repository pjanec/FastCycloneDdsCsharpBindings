using DdsMonitor.Engine;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DdsMonitor.Avalonia.FeatureDemoPlugin;

/// <summary>
/// Hosted service that publishes five DDS topic types at configurable rates,
/// exercising every field type the standard plugin supports.
/// </summary>
public sealed class DemoPublisherService : IHostedService, IDisposable
{
    private readonly ITopicRegistry _topicRegistry;
    private readonly IDdsBridge _ddsBridge;
    private readonly ILogger<DemoPublisherService>? _logger;

    private readonly bool _enabledAtStartup;

    private CancellationTokenSource? _cts;
    private Task? _allLoopsTask;
    private bool _publishing;
    private readonly object _toggleLock = new();

    // Deterministic seed for reproducible test output.
    private readonly Random _random = new(42);

    public bool IsPublishing
    {
        get { lock (_toggleLock) return _publishing; }
    }

    public DemoPublisherService(
        ITopicRegistry topicRegistry,
        IDdsBridge ddsBridge,
        IConfiguration configuration,
        ILogger<DemoPublisherService>? logger = null)
    {
        _topicRegistry = topicRegistry;
        _ddsBridge = ddsBridge;
        _logger = logger;

        _enabledAtStartup = configuration.GetValue("FeatureDemoPlugin:Enabled", true);
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Register all five topic types regardless of enabled state.
        RegisterTopics();

        if (_enabledAtStartup)
            StartPublishing();

        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        lock (_toggleLock)
        {
            _cts?.Cancel();
            _publishing = false;
        }

        if (_allLoopsTask is not null)
        {
            try
            {
                await _allLoopsTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException) { }
        }
    }

    /// <summary>Toggles publishing on or off at runtime (thread-safe).</summary>
    public void ToggleEnabled()
    {
        lock (_toggleLock)
        {
            if (_publishing)
                StopPublishing();
            else
                StartPublishing();
        }
    }

    private void RegisterTopics()
    {
        try
        {
            _topicRegistry.Register(new TopicMetadata(typeof(TelemetrySample)));
            _topicRegistry.Register(new TopicMetadata(typeof(EntityState)));
            _topicRegistry.Register(new TopicMetadata(typeof(AlertEvent)));
            _topicRegistry.Register(new TopicMetadata(typeof(GeoLocation)));
            _topicRegistry.Register(new TopicMetadata(typeof(UnionPayload)));
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "DemoPublisherService: failed to register topics.");
        }
    }

    private void StartPublishing()
    {
        _publishing = true;
        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        _allLoopsTask = Task.WhenAll(
            RunTelemetryLoopAsync(token),
            RunEntityStateLoopAsync(token),
            RunAlertLoopAsync(token),
            RunGeoLocationLoopAsync(token),
            RunUnionPayloadLoopAsync(token));
    }

    private void StopPublishing()
    {
        _publishing = false;
        _cts?.Cancel();
    }

    // ── Publish loops ─────────────────────────────────────────────────────────

    private async Task RunTelemetryLoopAsync(CancellationToken ct)
    {
        IDynamicWriter? writer = null;
        try
        {
            var meta = new TopicMetadata(typeof(TelemetrySample));
            writer = _ddsBridge.GetWriter(meta);

            double cpu = 30.0, mem = 40.0;
            float temp = 55.0f;
            int seq = 0;

            while (!ct.IsCancellationRequested)
            {
                cpu  = Math.Clamp(cpu  + (_random.NextDouble() - 0.5) * 4, 0, 100);
                mem  = Math.Clamp(mem  + (_random.NextDouble() - 0.5) * 4, 0, 100);
                temp = (float)Math.Clamp(temp + (_random.NextDouble() - 0.5) * 1, 20, 120);

                writer.Write(new TelemetrySample
                {
                    Timestamp  = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    SequenceId = seq++,
                    Cpu        = cpu,
                    Memory     = mem,
                    Temperature = temp,
                });

                await Task.Delay(100, ct).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) { _logger?.LogWarning(ex, "TelemetryLoop: write failed."); }
        finally { writer?.Dispose(); }
    }

    private async Task RunEntityStateLoopAsync(CancellationToken ct)
    {
        IDynamicWriter? writer = null;
        try
        {
            var meta = new TopicMetadata(typeof(EntityState));
            writer = _ddsBridge.GetWriter(meta);

            var positions = new float[8, 3]; // [entity, xyz]
            var health    = new byte[8];
            for (int i = 0; i < 8; i++) health[i] = 100;

            while (!ct.IsCancellationRequested)
            {
                for (int i = 0; i < 8; i++)
                {
                    positions[i, 0] += (float)(_random.NextDouble() - 0.5) * 2;
                    positions[i, 1] += (float)(_random.NextDouble() - 0.5) * 2;
                    positions[i, 2] += (float)(_random.NextDouble() - 0.5) * 0.1f;

                    if (health[i] > 0 && _random.NextDouble() < 0.02)
                        health[i] = (byte)Math.Max(0, health[i] - 5);

                    writer.Write(new EntityState
                    {
                        EntityId = i + 1,
                        Name     = $"Entity{i + 1}",
                        Kind     = (EntityKind)(i % 4),
                        X        = positions[i, 0],
                        Y        = positions[i, 1],
                        Z        = positions[i, 2],
                        Health   = health[i],
                        IsAlive  = health[i] > 0,
                    });
                }

                await Task.Delay(200, ct).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) { _logger?.LogWarning(ex, "EntityStateLoop: write failed."); }
        finally { writer?.Dispose(); }
    }

    private async Task RunAlertLoopAsync(CancellationToken ct)
    {
        IDynamicWriter? writer = null;
        try
        {
            var meta = new TopicMetadata(typeof(AlertEvent));
            writer = _ddsBridge.GetWriter(meta);

            int severityIndex = 0;
            var severities = (Severity[])Enum.GetValues(typeof(Severity));

            while (!ct.IsCancellationRequested)
            {
                var sev = severities[severityIndex % severities.Length];
                severityIndex++;

                writer.Write(new AlertEvent
                {
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    Level     = sev,
                    Message   = $"[{sev}] Demo alert #{severityIndex}",
                    Origin    = "DemoPublisher",
                });

                await Task.Delay(7000, ct).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) { _logger?.LogWarning(ex, "AlertLoop: write failed."); }
        finally { writer?.Dispose(); }
    }

    private async Task RunGeoLocationLoopAsync(CancellationToken ct)
    {
        IDynamicWriter? writer = null;
        try
        {
            var meta = new TopicMetadata(typeof(GeoLocation));
            writer = _ddsBridge.GetWriter(meta);

            double lat = 50.0, lon = 10.0;

            while (!ct.IsCancellationRequested)
            {
                lat = Math.Clamp(lat + (_random.NextDouble() - 0.5) * 0.01, 45, 55);
                lon = Math.Clamp(lon + (_random.NextDouble() - 0.5) * 0.01, 0, 20);

                writer.Write(new GeoLocation
                {
                    Latitude  = lat,
                    Longitude = lon,
                    Altitude  = (float)(_random.NextDouble() * 200),
                    NestedAddress = new Address
                    {
                        Street  = $"Demo Street {_random.Next(1, 100)}",
                        City    = "DemoCity",
                        Country = "DE",
                    },
                });

                await Task.Delay(2000, ct).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) { _logger?.LogWarning(ex, "GeoLocationLoop: write failed."); }
        finally { writer?.Dispose(); }
    }

    private async Task RunUnionPayloadLoopAsync(CancellationToken ct)
    {
        IDynamicWriter? writer = null;
        try
        {
            var meta = new TopicMetadata(typeof(UnionPayload));
            writer = _ddsBridge.GetWriter(meta);

            int discriminator = 0;

            while (!ct.IsCancellationRequested)
            {
                discriminator = (discriminator % 3) + 1;

                var payload = new UnionPayload { Discriminator = discriminator };
                switch (discriminator)
                {
                    case 1: payload.IntValue    = _random.Next();    break;
                    case 2: payload.StringValue = $"demo_{discriminator}"; break;
                    case 3: payload.DoubleValue = _random.NextDouble(); break;
                    default: payload.DefaultValue = true;            break;
                }

                writer.Write(payload);

                await Task.Delay(3000, ct).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) { _logger?.LogWarning(ex, "UnionPayloadLoop: write failed."); }
        finally { writer?.Dispose(); }
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
    }
}
