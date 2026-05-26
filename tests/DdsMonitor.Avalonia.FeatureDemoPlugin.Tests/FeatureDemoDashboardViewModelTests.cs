using System;
using System.Collections.Generic;
using DdsMonitor.Engine;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace DdsMonitor.Avalonia.FeatureDemoPlugin.Tests;

public sealed class FeatureDemoDashboardViewModelTests
{
    // ── Fake ISampleStore ─────────────────────────────────────────────────────

    private sealed class FakeSampleStore : ISampleStore
    {
        private readonly Dictionary<Type, int> _counts = new();

        public void SetCount(Type topicType, int count) => _counts[topicType] = count;

        // ISampleStore
        public IReadOnlyList<SampleData> AllSamples => [];
        public int TotalCount => 0;
        public long TotalBytesReceived => 0;
        public event Action? Cleared;

        public SampleData[] GetSamples(int startIndex) => [];

        public ITopicSamples GetTopicSamples(Type topicType) =>
            new EmptyTopicSamples(topicType);

        public int GetTopicCount(Type topicType) =>
            _counts.TryGetValue(topicType, out var c) ? c : 0;

        public void Append(SampleData sample) { }
        public void Clear() => Cleared?.Invoke();
    }

    private sealed class EmptyTopicSamples : ITopicSamples
    {
        public EmptyTopicSamples(Type topicType) => TopicType = topicType;
        public Type TopicType { get; }
        public int TotalCount => 0;
        public IReadOnlyList<SampleData> Samples => [];
    }

    // ── Fake DemoPublisherService helpers ─────────────────────────────────────

    private static DemoPublisherService CreatePublisher(bool enabled = false)
    {
        var registry = new NoOpTopicRegistry();
        var bridge   = new NoOpDdsBridge();
        var config   = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
                            .AddInMemoryCollection(new Dictionary<string, string?>
                            {
                                ["FeatureDemoPlugin:Enabled"] = enabled ? "true" : "false",
                            })
                            .Build();

        return new DemoPublisherService(registry, bridge, config);
    }

    private sealed class NoOpTopicRegistry : ITopicRegistry
    {
        public event Action? Changed;
        public IReadOnlyList<TopicMetadata> AllTopics => [];
        public TopicMetadata? GetByType(Type t) => null;
        public TopicMetadata? GetByName(string n) => null;
        public void Register(TopicMetadata meta) => Changed?.Invoke();
    }

    private sealed class NoOpDdsBridge : IDdsBridge
    {
        public CycloneDDS.Runtime.DdsParticipant Participant => null!;
        public IReadOnlyList<CycloneDDS.Runtime.DdsParticipant> Participants => [];
        public IReadOnlyList<ParticipantConfig> ParticipantConfigs => [];
        public string? CurrentPartition => null;
        public bool IsPaused { get; set; }
        public IReadOnlyDictionary<Type, IDynamicReader> ActiveReaders =>
            new Dictionary<Type, IDynamicReader>();
        public IReadOnlySet<Type> ExplicitlyUnsubscribedTopicTypes => new HashSet<Type>();
        public event Action? ReadersChanged;

        public IDynamicWriter GetWriter(TopicMetadata meta) => new NullWriter(meta.TopicType);
        public IDynamicReader Subscribe(TopicMetadata meta) => null!;
        public bool TrySubscribe(TopicMetadata meta, out IDynamicReader? reader, out string? errorMessage)
        {
            reader = null; errorMessage = null; return false;
        }
        public void Unsubscribe(TopicMetadata meta) { }
        public void ChangePartition(string? p) { }
        public void InitializeExplicitlyUnsubscribed(IEnumerable<Type> types) { }
        public void AddParticipant(uint d, string p) { }
        public void RemoveParticipant(int i) { }
        public void ResetAll() { }
        public void Dispose() { }

        private sealed class NullWriter : IDynamicWriter
        {
            public NullWriter(Type t) => TopicType = t;
            public Type TopicType { get; }
            public void Write(object payload) { }
            public void DisposeInstance(object payload) { }
            public void Dispose() { }
        }
    }

    // ── Tests ─────────────────────────────────────────────────────────────────

    [Fact]
    public void TopicRows_ReflectSampleStoreCount()
    {
        var store = new FakeSampleStore();
        store.SetCount(typeof(TelemetrySample), 42);
        store.SetCount(typeof(EntityState),     17);
        store.SetCount(typeof(AlertEvent),       3);
        store.SetCount(typeof(GeoLocation),      8);
        store.SetCount(typeof(UnionPayload),      5);

        var publisher = CreatePublisher(enabled: false);
        var vm = new FeatureDemoDashboardViewModel(store, publisher);

        vm.Tick();

        Assert.Equal(5, vm.TopicRows.Count);
        Assert.Equal(42, vm.TopicRows[0].Count); // TelemetrySample
        Assert.Equal(17, vm.TopicRows[1].Count); // EntityState
        Assert.Equal(3,  vm.TopicRows[2].Count); // AlertEvent
        Assert.Equal(8,  vm.TopicRows[3].Count); // GeoLocation
        Assert.Equal(5,  vm.TopicRows[4].Count); // UnionPayload
    }

    [Fact]
    public void PublisherStateLabel_ReflectsToggle()
    {
        var store     = new FakeSampleStore();
        var publisher = CreatePublisher(enabled: false);

        var vm = new FeatureDemoDashboardViewModel(store, publisher);

        // Initially not publishing (config disabled).
        Assert.Equal("Stopped", vm.PublisherStateLabel);

        // Toggle on.
        publisher.ToggleEnabled();
        Assert.Equal("Publishing", vm.PublisherStateLabel);

        // Toggle off.
        publisher.ToggleEnabled();
        Assert.Equal("Stopped", vm.PublisherStateLabel);
    }
}
