using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;

namespace DdsMonitor.Avalonia.FeatureDemoPlugin;

/// <summary>
/// Code-behind for <see cref="FeatureDemoDashboardView"/>.
/// Starts and stops a 1 Hz DispatcherTimer that calls <see cref="FeatureDemoDashboardViewModel.Tick"/>
/// when the control is attached to and detached from the visual tree.
/// </summary>
public partial class FeatureDemoDashboardView : UserControl
{
    private DispatcherTimer? _timer;

    public FeatureDemoDashboardView()
    {
        InitializeComponent();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += OnTimerTick;
        _timer.Start();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _timer?.Stop();
        _timer = null;

        base.OnDetachedFromVisualTree(e);
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        if (DataContext is FeatureDemoDashboardViewModel vm)
            vm.Tick();
    }
}
