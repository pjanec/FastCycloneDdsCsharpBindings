using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using DdsMonitor.Avalonia.Core;

namespace DdsMonitor.Avalonia.Controls;

// ── Resize edge flags ─────────────────────────────────────────────────────────

/// <summary>
/// Identifies which edge or corner of an <see cref="MdiChild"/> is being resized.
/// Corner directions are a bitwise OR of two edges (e.g. Top | Right = top-right corner).
/// </summary>
[Flags]
public enum ResizeEdge
{
    None   = 0,
    Top    = 1,
    Right  = 2,
    Bottom = 4,
    Left   = 8,
}

// ── Custom event args ─────────────────────────────────────────────────────────

/// <summary>
/// Event args raised when the user requests a drag (move) operation.
/// The host applies the delta to <c>Canvas.Left</c> / <c>Canvas.Top</c>.
/// </summary>
public sealed class MdiChildDragEventArgs : RoutedEventArgs
{
    /// <summary>Horizontal delta in pixels.</summary>
    public double DeltaX { get; init; }
    /// <summary>Vertical delta in pixels.</summary>
    public double DeltaY { get; init; }
    /// <summary>Whether Shift is held (snap-to-grid requested).</summary>
    public bool SnapToGrid { get; init; }

    public MdiChildDragEventArgs(RoutedEvent routedEvent) : base(routedEvent) { }
}

/// <summary>
/// Event args raised when the user requests a resize operation.
/// The host applies width/height adjustments respecting minimum sizes.
/// </summary>
public sealed class MdiChildResizeEventArgs : RoutedEventArgs
{
    /// <summary>Which edge(s) are being resized.</summary>
    public ResizeEdge Edge { get; init; }
    /// <summary>Horizontal delta in pixels.</summary>
    public double DeltaX { get; init; }
    /// <summary>Vertical delta in pixels.</summary>
    public double DeltaY { get; init; }

    public MdiChildResizeEventArgs(RoutedEvent routedEvent) : base(routedEvent) { }
}

/// <summary>
/// Event args raised when the user requests a layout-kind change via the
/// titlebar context menu (e.g. "Dock as tab", "Dock as left tool").
/// </summary>
public sealed class MdiChildLayoutKindEventArgs : RoutedEventArgs
{
    /// <summary>The requested target <see cref="LayoutKind"/>.</summary>
    public LayoutKind TargetKind { get; init; }

    /// <summary>Optional side hint for tool-dock requests (not used for <see cref="LayoutKind.DockDocument"/>).</summary>
    public string? SideHint { get; init; }

    public MdiChildLayoutKindEventArgs(RoutedEvent routedEvent) : base(routedEvent) { }
}

// ── MdiChild ──────────────────────────────────────────────────────────────────

/// <summary>
/// A floating child window that lives inside an <see cref="MdiHost"/> canvas.
/// Provides a titlebar, resize handles, drag/resize event delegation, z-order
/// contribution, and a layout context menu.
/// </summary>
/// <remarks>
/// <para>
/// This control does <b>not</b> apply position or size changes itself.  Instead
/// it fires <see cref="DragRequested"/> and <see cref="ResizeRequested"/> routed
/// events.  The host subscribes to these events and updates
/// <c>Canvas.Left</c> / <c>Canvas.Top</c> / <c>Width</c> / <c>Height</c>.
/// </para>
/// <para>Zero references to Dock.NET in this file.</para>
/// </remarks>
public sealed class MdiChild : ContentControl
{
    // ── Snap-grid constant ────────────────────────────────────────────────────

    private const double SnapGrid = 8.0;

    // ── Styled properties ─────────────────────────────────────────────────────

    /// <summary>Title shown in the titlebar.</summary>
    public static readonly StyledProperty<string> TitleProperty =
        AvaloniaProperty.Register<MdiChild, string>(nameof(Title), defaultValue: string.Empty);

    /// <summary>Whether this child is the currently active (focused) one.</summary>
    public static readonly StyledProperty<bool> IsActiveProperty =
        AvaloniaProperty.Register<MdiChild, bool>(nameof(IsActive), defaultValue: false);

    /// <summary>Whether this child is minimised.</summary>
    public static readonly StyledProperty<bool> IsMinimisedProperty =
        AvaloniaProperty.Register<MdiChild, bool>(nameof(IsMinimised), defaultValue: false);

    /// <summary>Optional content placed on the right side of the titlebar.</summary>
    public static readonly StyledProperty<object?> TitlebarExtrasProperty =
        AvaloniaProperty.Register<MdiChild, object?>(nameof(TitlebarExtras));

    /// <summary>Opaque identifier used by the host for tracking.</summary>
    public static readonly StyledProperty<string> ChildIdProperty =
        AvaloniaProperty.Register<MdiChild, string>(nameof(ChildId), defaultValue: string.Empty);

    // ── Routed events ─────────────────────────────────────────────────────────

    /// <summary>Raised when the user clicks the close button.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> CloseRequestedEvent =
        RoutedEvent.Register<MdiChild, RoutedEventArgs>(nameof(CloseRequested), RoutingStrategies.Bubble);

    /// <summary>Raised when the user clicks the minimise button.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> MinimiseRequestedEvent =
        RoutedEvent.Register<MdiChild, RoutedEventArgs>(nameof(MinimiseRequested), RoutingStrategies.Bubble);

    /// <summary>Raised when the user selects a "Dock as …" item in the context menu.</summary>
    public static readonly RoutedEvent<MdiChildLayoutKindEventArgs> LayoutKindRequestedEvent =
        RoutedEvent.Register<MdiChild, MdiChildLayoutKindEventArgs>(nameof(LayoutKindRequested), RoutingStrategies.Bubble);

    /// <summary>Raised on each pointer move during a drag. The host applies the delta.</summary>
    public static readonly RoutedEvent<MdiChildDragEventArgs> DragRequestedEvent =
        RoutedEvent.Register<MdiChild, MdiChildDragEventArgs>(nameof(DragRequested), RoutingStrategies.Bubble);

    /// <summary>Raised when the user presses Escape to cancel an in-progress drag.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> DragCancelledEvent =
        RoutedEvent.Register<MdiChild, RoutedEventArgs>(nameof(DragCancelled), RoutingStrategies.Bubble);

    /// <summary>Raised on each pointer move during a resize. The host applies the delta.</summary>
    public static readonly RoutedEvent<MdiChildResizeEventArgs> ResizeRequestedEvent =
        RoutedEvent.Register<MdiChild, MdiChildResizeEventArgs>(nameof(ResizeRequested), RoutingStrategies.Bubble);

    /// <summary>Raised when the child requests to be brought to the top of the z-order.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> BringToFrontRequestedEvent =
        RoutedEvent.Register<MdiChild, RoutedEventArgs>(nameof(BringToFrontRequested), RoutingStrategies.Bubble);

    // ── CLR event wrappers ────────────────────────────────────────────────────

    /// <inheritdoc cref="CloseRequestedEvent"/>
    public event EventHandler<RoutedEventArgs> CloseRequested
    {
        add    => AddHandler(CloseRequestedEvent, value);
        remove => RemoveHandler(CloseRequestedEvent, value);
    }

    /// <inheritdoc cref="MinimiseRequestedEvent"/>
    public event EventHandler<RoutedEventArgs> MinimiseRequested
    {
        add    => AddHandler(MinimiseRequestedEvent, value);
        remove => RemoveHandler(MinimiseRequestedEvent, value);
    }

    /// <inheritdoc cref="LayoutKindRequestedEvent"/>
    public event EventHandler<MdiChildLayoutKindEventArgs> LayoutKindRequested
    {
        add    => AddHandler(LayoutKindRequestedEvent, value);
        remove => RemoveHandler(LayoutKindRequestedEvent, value);
    }

    /// <inheritdoc cref="DragRequestedEvent"/>
    public event EventHandler<MdiChildDragEventArgs> DragRequested
    {
        add    => AddHandler(DragRequestedEvent, value);
        remove => RemoveHandler(DragRequestedEvent, value);
    }

    /// <inheritdoc cref="DragCancelledEvent"/>
    public event EventHandler<RoutedEventArgs> DragCancelled
    {
        add    => AddHandler(DragCancelledEvent, value);
        remove => RemoveHandler(DragCancelledEvent, value);
    }

    /// <inheritdoc cref="ResizeRequestedEvent"/>
    public event EventHandler<MdiChildResizeEventArgs> ResizeRequested
    {
        add    => AddHandler(ResizeRequestedEvent, value);
        remove => RemoveHandler(ResizeRequestedEvent, value);
    }

    /// <inheritdoc cref="BringToFrontRequestedEvent"/>
    public event EventHandler<RoutedEventArgs> BringToFrontRequested
    {
        add    => AddHandler(BringToFrontRequestedEvent, value);
        remove => RemoveHandler(BringToFrontRequestedEvent, value);
    }

    // ── Properties ────────────────────────────────────────────────────────────

    /// <inheritdoc cref="TitleProperty"/>
    public string Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    /// <inheritdoc cref="IsActiveProperty"/>
    public bool IsActive
    {
        get => GetValue(IsActiveProperty);
        set => SetValue(IsActiveProperty, value);
    }

    /// <inheritdoc cref="IsMinimisedProperty"/>
    public bool IsMinimised
    {
        get => GetValue(IsMinimisedProperty);
        set => SetValue(IsMinimisedProperty, value);
    }

    /// <inheritdoc cref="TitlebarExtrasProperty"/>
    public object? TitlebarExtras
    {
        get => GetValue(TitlebarExtrasProperty);
        set => SetValue(TitlebarExtrasProperty, value);
    }

    /// <inheritdoc cref="ChildIdProperty"/>
    public string ChildId
    {
        get => GetValue(ChildIdProperty);
        set => SetValue(ChildIdProperty, value);
    }

    // ── Drag / resize state ───────────────────────────────────────────────────

    private enum InteractionMode { None, Drag, Resize }

    private InteractionMode _mode = InteractionMode.None;
    private ResizeEdge _resizeEdge = ResizeEdge.None;
    private Point _startPointer;

    // ── Template parts ────────────────────────────────────────────────────────

    private Border? _titlebar;
    private Button? _minimiseButton;
    private Button? _closeButton;

    // ── Constructor ───────────────────────────────────────────────────────────

    static MdiChild()
    {
        IsActiveProperty.Changed.AddClassHandler<MdiChild>((child, _) => child.UpdateActiveClass());
    }

    // ── Public methods ────────────────────────────────────────────────────────

    /// <summary>Fires <see cref="BringToFrontRequestedEvent"/> to request z-order promotion.</summary>
    public void BringToFront()
    {
        RaiseEvent(new RoutedEventArgs(BringToFrontRequestedEvent));
    }

    // ── Template application ──────────────────────────────────────────────────

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        // Detach old handlers if the template is re-applied.
        if (_titlebar is not null)
        {
            _titlebar.PointerPressed  -= OnTitlebarPointerPressed;
            _titlebar.PointerMoved    -= OnTitlebarPointerMoved;
            _titlebar.PointerReleased -= OnTitlebarPointerReleased;
        }

        _titlebar        = e.NameScope.Find<Border>("PART_Titlebar");
        _minimiseButton  = e.NameScope.Find<Button>("PART_MinimiseButton");
        _closeButton     = e.NameScope.Find<Button>("PART_CloseButton");

        if (_titlebar is not null)
        {
            _titlebar.PointerPressed  += OnTitlebarPointerPressed;
            _titlebar.PointerMoved    += OnTitlebarPointerMoved;
            _titlebar.PointerReleased += OnTitlebarPointerReleased;
        }

        if (_minimiseButton is not null)
            _minimiseButton.Click += (_, _) => RaiseEvent(new RoutedEventArgs(MinimiseRequestedEvent));

        if (_closeButton is not null)
            _closeButton.Click += (_, _) => RaiseEvent(new RoutedEventArgs(CloseRequestedEvent));

        // Wire resize handles by name prefix.
        WireResizeHandles(e.NameScope);

        // Context menu on the titlebar.
        if (_titlebar is not null)
            _titlebar.ContextMenu = BuildTitlebarContextMenu();

        UpdateActiveClass();
    }

    // ── Keyboard ──────────────────────────────────────────────────────────────

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Key == Key.Escape && _mode != InteractionMode.None)
        {
            CancelInteraction();
            e.Handled = true;
        }
    }

    // ── Titlebar drag ─────────────────────────────────────────────────────────

    private void OnTitlebarPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;

        // Click anywhere → raise to front.
        BringToFront();

        _mode         = InteractionMode.Drag;
        _startPointer = e.GetPosition(Parent as Visual);
        e.Pointer.Capture(_titlebar);
        e.Handled = true;
    }

    private void OnTitlebarPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_mode != InteractionMode.Drag) return;

        var current = e.GetPosition(Parent as Visual);
        var dx = current.X - _startPointer.X;
        var dy = current.Y - _startPointer.Y;

        if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
        {
            dx = Math.Round(dx / SnapGrid) * SnapGrid;
            dy = Math.Round(dy / SnapGrid) * SnapGrid;
        }

        RaiseEvent(new MdiChildDragEventArgs(DragRequestedEvent)
        {
            DeltaX      = dx,
            DeltaY      = dy,
            SnapToGrid  = e.KeyModifiers.HasFlag(KeyModifiers.Shift),
        });

        _startPointer = current;
        e.Handled = true;
    }

    private void OnTitlebarPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_mode != InteractionMode.Drag) return;
        _mode = InteractionMode.None;
        e.Pointer.Capture(null);
        e.Handled = true;
    }

    // ── Resize handles ────────────────────────────────────────────────────────

    private void WireResizeHandles(INameScope scope)
    {
        var handles = new[]
        {
            ("PART_ResizeTop",         ResizeEdge.Top),
            ("PART_ResizeRight",       ResizeEdge.Right),
            ("PART_ResizeBottom",      ResizeEdge.Bottom),
            ("PART_ResizeLeft",        ResizeEdge.Left),
            ("PART_ResizeTopLeft",     ResizeEdge.Top | ResizeEdge.Left),
            ("PART_ResizeTopRight",    ResizeEdge.Top | ResizeEdge.Right),
            ("PART_ResizeBottomLeft",  ResizeEdge.Bottom | ResizeEdge.Left),
            ("PART_ResizeBottomRight", ResizeEdge.Bottom | ResizeEdge.Right),
        };

        foreach (var (name, edge) in handles)
        {
            var handle = scope.Find<Border>(name);
            if (handle is null) continue;

            var capturedEdge = edge;
            handle.PointerPressed  += (s, e) => OnResizeHandlePressed(s, e, capturedEdge);
            handle.PointerMoved    += OnResizeHandleMoved;
            handle.PointerReleased += OnResizeHandleReleased;
        }
    }

    private void OnResizeHandlePressed(object? sender, PointerPressedEventArgs e, ResizeEdge edge)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;

        BringToFront();
        _mode         = InteractionMode.Resize;
        _resizeEdge   = edge;
        _startPointer = e.GetPosition(Parent as Visual);

        if (sender is InputElement el)
            e.Pointer.Capture(el);

        e.Handled = true;
    }

    private void OnResizeHandleMoved(object? sender, PointerEventArgs e)
    {
        if (_mode != InteractionMode.Resize) return;

        var current = e.GetPosition(Parent as Visual);
        var dx = current.X - _startPointer.X;
        var dy = current.Y - _startPointer.Y;

        RaiseEvent(new MdiChildResizeEventArgs(ResizeRequestedEvent)
        {
            Edge   = _resizeEdge,
            DeltaX = dx,
            DeltaY = dy,
        });

        _startPointer = current;
        e.Handled = true;
    }

    private void OnResizeHandleReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_mode != InteractionMode.Resize) return;
        _mode = InteractionMode.None;
        _resizeEdge = ResizeEdge.None;
        e.Pointer.Capture(null);
        e.Handled = true;
    }

    // ── Cancel interaction ────────────────────────────────────────────────────

    private void CancelInteraction()
    {
        if (_mode == InteractionMode.Drag)
        {
            RaiseEvent(new RoutedEventArgs(DragCancelledEvent));
        }
        _mode = InteractionMode.None;
        _resizeEdge = ResizeEdge.None;
    }

    // ── Context menu ──────────────────────────────────────────────────────────

    private ContextMenu BuildTitlebarContextMenu()
    {
        var menu = new ContextMenu();
        menu.Items.Add(MakeMenuItem("_Dock as tab",          () => RaiseLayoutKind(LayoutKind.DockDocument)));
        menu.Items.Add(MakeMenuItem("Dock as _left tool",    () => RaiseLayoutKind(LayoutKind.DockTool, "left")));
        menu.Items.Add(MakeMenuItem("Dock as _right tool",   () => RaiseLayoutKind(LayoutKind.DockTool, "right")));
        menu.Items.Add(MakeMenuItem("Dock as _bottom tool",  () => RaiseLayoutKind(LayoutKind.DockTool, "bottom")));
        menu.Items.Add(new Separator());
        menu.Items.Add(MakeMenuItem("Mi_nimise", () => RaiseEvent(new RoutedEventArgs(MinimiseRequestedEvent))));
        menu.Items.Add(MakeMenuItem("_Close",    () => RaiseEvent(new RoutedEventArgs(CloseRequestedEvent))));
        return menu;
    }

    private static MenuItem MakeMenuItem(string header, Action action)
    {
        var item = new MenuItem { Header = header };
        item.Click += (_, _) => action();
        return item;
    }

    private void RaiseLayoutKind(LayoutKind kind, string? sideHint = null)
    {
        RaiseEvent(new MdiChildLayoutKindEventArgs(LayoutKindRequestedEvent)
        {
            TargetKind = kind,
            SideHint   = sideHint,
        });
    }

    // ── Active visual class ───────────────────────────────────────────────────

    private void UpdateActiveClass()
    {
        if (IsActive)
            Classes.Add("active");
        else
            Classes.Remove("active");
    }
}
