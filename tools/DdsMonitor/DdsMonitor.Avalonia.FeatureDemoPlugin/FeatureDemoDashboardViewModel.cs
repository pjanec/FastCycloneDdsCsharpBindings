using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;
using DdsMonitor.Avalonia.Core;
using DdsMonitor.Engine;

namespace DdsMonitor.Avalonia.FeatureDemoPlugin;

/// <summary>
/// Row for the topic-count table in the Feature Demo dashboard.
/// </summary>
/// <param name="Name">Human-readable topic name.</param>
/// <param name="Count">Number of samples received so far.</param>
public sealed record TopicCountRow(string Name, int Count);

/// <summary>
/// View-model for the Feature Demo dashboard panel.
/// Displays per-topic sample counts and recent alert messages.
/// </summary>
public sealed class FeatureDemoDashboardViewModel : IStatefulViewModel
{
    private readonly ISampleStore _sampleStore;
    private readonly DemoPublisherService _publisher;

    private IReadOnlyList<TopicCountRow> _topicRows = [];

    // ── Properties ────────────────────────────────────────────────────────────

    public string PublisherStateLabel =>
        _publisher.IsPublishing ? "Publishing" : "Stopped";

    public IReadOnlyList<TopicCountRow> TopicRows => _topicRows;

    public ObservableCollection<string> RecentAlerts { get; } = [];

    public ICommand TogglePublisherCommand { get; }

    // ── Construction ──────────────────────────────────────────────────────────

    public FeatureDemoDashboardViewModel(ISampleStore sampleStore, DemoPublisherService publisher)
    {
        _sampleStore = sampleStore;
        _publisher   = publisher;

        TogglePublisherCommand = new RelayCommand(() =>
        {
            _publisher.ToggleEnabled();
            // Notify the view that the label changed (fine-grained; VM is not INPC here).
        });
    }

    // ── IStatefulViewModel ────────────────────────────────────────────────────

    public void Initialize(IDictionary<string, object> componentState)
    {
        Tick();
    }

    // ── Refresh ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Reads the current sample counts from the store and refreshes <see cref="TopicRows"/>.
    /// Called once per second by the view's DispatcherTimer.
    /// </summary>
    public void Tick()
    {
        _topicRows = new List<TopicCountRow>
        {
            new("Telemetry",   _sampleStore.GetTopicCount(typeof(TelemetrySample))),
            new("EntityState", _sampleStore.GetTopicCount(typeof(EntityState))),
            new("Alert",       _sampleStore.GetTopicCount(typeof(AlertEvent))),
            new("GeoLocation", _sampleStore.GetTopicCount(typeof(GeoLocation))),
            new("UnionPayload",_sampleStore.GetTopicCount(typeof(UnionPayload))),
        };

        // Harvest new alerts from the sample store.
        var topicSamples = _sampleStore.GetTopicSamples(typeof(AlertEvent));
        if (topicSamples.TotalCount > 0)
        {
            int prev = RecentAlerts.Count;
            foreach (var sample in topicSamples.Samples.Skip(prev))
            {
                if (sample.Payload is AlertEvent alert)
                    RecentAlerts.Add($"[{alert.Level}] {alert.Message}");

                while (RecentAlerts.Count > 10)
                    RecentAlerts.RemoveAt(0);
            }
        }
    }

    // ── Minimal ICommand implementation ───────────────────────────────────────

    private sealed class RelayCommand : ICommand
    {
        private readonly Action _execute;
        public RelayCommand(Action execute) => _execute = execute;
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter) => _execute();
        public event EventHandler? CanExecuteChanged { add { } remove { } }
    }
}
