using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DdsMonitor.Engine;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace DdsMonitor.Avalonia.FeatureDemoPlugin.Tests;

public sealed class DemoPublisherServiceTests
{
    // ── Fake ITopicRegistry ───────────────────────────────────────────────────

    private sealed class CapturingTopicRegistry : ITopicRegistry
    {
        public List<TopicMetadata> Registrations { get; } = [];

        public event Action? Changed;

        public IReadOnlyList<TopicMetadata> AllTopics => Registrations;

        public TopicMetadata? GetByType(Type topicType) =>
            Registrations.Find(m => m.TopicType == topicType);

        public TopicMetadata? GetByName(string topicName) =>
            Registrations.Find(m => m.TopicName == topicName);

        public void Register(TopicMetadata meta)
        {
            Registrations.Add(meta);
            Changed?.Invoke();
        }
    }

    // ── Fake IDynamicWriter ───────────────────────────────────────────────────

    private sealed class CountingWriter : IDynamicWriter
    {
        public Type TopicType { get; }
        public int WriteCount { get; private set; }

        public CountingWriter(Type topicType) => TopicType = topicType;

        public void Write(object payload) => WriteCount++;
        public void DisposeInstance(object payload) { }
        public void Dispose() { }
    }

    // ── Fake IDdsBridge ───────────────────────────────────────────────────────

    private sealed class RecordingDdsBridge : IDdsBridge
    {
        private readonly Dictionary<Type, CountingWriter> _writers = new();

        public CountingWriter GetCountingWriter(Type t) => _writers[t];

        // IDdsBridge
        public CycloneDDS.Runtime.DdsParticipant Participant => null!;
        public IReadOnlyList<CycloneDDS.Runtime.DdsParticipant> Participants => [];
        public IReadOnlyList<ParticipantConfig> ParticipantConfigs => [];
        public string? CurrentPartition => null;
        public bool IsPaused { get; set; }
        public IReadOnlyDictionary<Type, IDynamicReader> ActiveReaders =>
            new Dictionary<Type, IDynamicReader>();
        public IReadOnlySet<Type> ExplicitlyUnsubscribedTopicTypes => new HashSet<Type>();
        public event Action? ReadersChanged;

        public IDynamicWriter GetWriter(TopicMetadata meta)
        {
            if (!_writers.TryGetValue(meta.TopicType, out var w))
            {
                w = new CountingWriter(meta.TopicType);
                _writers[meta.TopicType] = w;
            }
            return w;
        }

        public IDynamicReader Subscribe(TopicMetadata meta) => null!;
        public bool TrySubscribe(TopicMetadata meta, out IDynamicReader? reader, out string? errorMessage)
        {
            reader = null;
            errorMessage = null;
            return false;
        }
        public void Unsubscribe(TopicMetadata meta) { }
        public void ChangePartition(string? newPartition) { }
        public void InitializeExplicitlyUnsubscribed(IEnumerable<Type> types) { }
        public void AddParticipant(uint domainId, string partitionName) { }
        public void RemoveParticipant(int participantIndex) { }
        public void ResetAll() { }
        public void Dispose() { }
    }

    // ── Tests ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task DemoPublisherService_Publishes_AllFiveTopics()
    {
        var registry = new CapturingTopicRegistry();
        var bridge   = new RecordingDdsBridge();
        var config   = new ConfigurationBuilder().Build();

        var svc = new DemoPublisherService(registry, bridge, config);
        await svc.StartAsync(CancellationToken.None);

        await Task.Delay(500);

        await svc.StopAsync(CancellationToken.None);

        // Five distinct topic types must have been registered.
        var distinctTypes = new HashSet<Type>(registry.Registrations.ConvertAll(m => m.TopicType));
        Assert.Equal(5, distinctTypes.Count);
        Assert.Contains(typeof(TelemetrySample), distinctTypes);
        Assert.Contains(typeof(EntityState),     distinctTypes);
        Assert.Contains(typeof(AlertEvent),      distinctTypes);
        Assert.Contains(typeof(GeoLocation),     distinctTypes);
        Assert.Contains(typeof(UnionPayload),    distinctTypes);

        // Telemetry runs at 100 ms, so at least 1 write in 500 ms.
        var telemetryWriter = bridge.GetCountingWriter(typeof(TelemetrySample));
        Assert.True(telemetryWriter.WriteCount >= 1,
            $"Expected at least 1 telemetry write, got {telemetryWriter.WriteCount}.");
    }

    [Fact]
    public async Task DemoPublisherService_ToggleEnabled_StopsPublishing()
    {
        var registry = new CapturingTopicRegistry();
        var bridge   = new RecordingDdsBridge();
        var config   = new ConfigurationBuilder().Build();

        var svc = new DemoPublisherService(registry, bridge, config);
        await svc.StartAsync(CancellationToken.None);

        await Task.Delay(250);

        // Toggle off.
        svc.ToggleEnabled();

        // Record count immediately after stop.
        var countAfterStop = bridge.GetCountingWriter(typeof(TelemetrySample)).WriteCount;

        // Wait 300 ms more — no further writes should have occurred.
        await Task.Delay(300);

        var countAfterDelay = bridge.GetCountingWriter(typeof(TelemetrySample)).WriteCount;

        await svc.StopAsync(CancellationToken.None);

        Assert.Equal(countAfterStop, countAfterDelay);
    }
}
