using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace DdsMonitor.Avalonia.Controls;

// ── Child geometry event args ─────────────────────────────────────────────────

/// <summary>
/// Raised when a child's position, size, or minimised state changes, so the
/// workspace manager can debounce-save the new geometry.
/// </summary>
public sealed class MdiChildGeometryChangedEventArgs : EventArgs
{
    /// <summary>The child whose geometry changed.</summary>
    public string ChildId { get; init; } = string.Empty;
    /// <summary>New bounding rectangle in host-local coordinates.</summary>
    public Rect NewBounds { get; init; }
    /// <summary>Whether the child is currently minimised.</summary>
    public bool IsMinimised { get; init; }
}

// ── MdiHost ───────────────────────────────────────────────────────────────────

/// <summary>
/// A canvas-based host for multiple <see cref="MdiChild"/> instances.  Manages
/// z-order, the minimised-panel strip, drag and resize application, and boundary
/// clamping.
/// </summary>
/// <remarks>
/// <para>
/// Children are added imperatively via <see cref="Add"/> and removed via
/// <see cref="Remove"/>.  Positioning uses <c>Canvas.Left</c> / <c>Canvas.Top</c>
/// which is applied by this host in response to the child's
/// <see cref="MdiChild.DragRequestedEvent"/> and
/// <see cref="MdiChild.ResizeRequestedEvent"/>.
/// </para>
/// <para>Zero references to Dock.NET in this file.</para>
/// </remarks>
public sealed class MdiHost : TemplatedControl
{
    // ── Constants ─────────────────────────────────────────────────────────────

    private const double MinWidth     = 220.0;
    private const double MinHeight    = 140.0;
    /// <summary>Minimum pixels of titlebar (28 px) that must remain inside the host.</summary>
    private const double MinTitlebarVisible = 40.0;

    // ── Template parts ────────────────────────────────────────────────────────

    private Canvas?    _childCanvas;
    private ItemsControl? _minimisedStrip;

    // ── State ─────────────────────────────────────────────────────────────────

    private readonly Dictionary<string, MdiChild> _children     = new();
    private readonly Dictionary<string, Button>   _stripButtons = new();
    private int _zCounter;

    // ── Events ────────────────────────────────────────────────────────────────

    /// <summary>Raised when a child is added.</summary>
    public event EventHandler<MdiChild>? ChildAdded;
    /// <summary>Raised when a child is removed.</summary>
    public event EventHandler<string>? ChildRemoved;
    /// <summary>Raised after every drag or resize operation ends.</summary>
    public event EventHandler<MdiChildGeometryChangedEventArgs>? ChildGeometryChanged;

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Snapshot of managed children, ordered bottom-z-order first (top-of-z-order last).
    /// </summary>
    public IReadOnlyList<MdiChild> Children =>
        _children.Values
                 .OrderBy(c => c.ZIndex)
                 .ToList();

    /// <summary>Adds a child at the given position and brings it to front.</summary>
    public void Add(MdiChild child, double x, double y, double width, double height)
    {
        ArgumentNullException.ThrowIfNull(child);

        if (string.IsNullOrEmpty(child.ChildId))
            child.ChildId = Guid.NewGuid().ToString("N");

        _children[child.ChildId] = child;

        // Wire host-side handlers.
        child.AddHandler(MdiChild.DragRequestedEvent,      OnChildDragRequested);
        child.AddHandler(MdiChild.ResizeRequestedEvent,    OnChildResizeRequested);
        child.AddHandler(MdiChild.CloseRequestedEvent,     OnChildCloseRequested);
        child.AddHandler(MdiChild.MinimiseRequestedEvent,  OnChildMinimiseRequested);
        child.AddHandler(MdiChild.BringToFrontRequestedEvent, OnChildBringToFrontRequested);

        // Set initial geometry.
        child.Width  = Math.Max(MinWidth,  width);
        child.Height = Math.Max(MinHeight, height);

        if (_childCanvas is not null)
        {
            Canvas.SetLeft(child, x);
            Canvas.SetTop(child,  y);
            _childCanvas.Children.Add(child);
        }

        BringToFront(child.ChildId);
        ChildAdded?.Invoke(this, child);
    }

    /// <summary>Removes a child by id.  Returns false if not found.</summary>
    public bool Remove(string childId)
    {
        if (!_children.TryGetValue(childId, out var child)) return false;

        child.RemoveHandler(MdiChild.DragRequestedEvent,      OnChildDragRequested);
        child.RemoveHandler(MdiChild.ResizeRequestedEvent,    OnChildResizeRequested);
        child.RemoveHandler(MdiChild.CloseRequestedEvent,     OnChildCloseRequested);
        child.RemoveHandler(MdiChild.MinimiseRequestedEvent,  OnChildMinimiseRequested);
        child.RemoveHandler(MdiChild.BringToFrontRequestedEvent, OnChildBringToFrontRequested);

        _childCanvas?.Children.Remove(child);

        // Remove from strip if minimised.
        if (_stripButtons.TryGetValue(childId, out var btn))
        {
            if (_minimisedStrip is not null)
                _minimisedStrip.Items.Remove(btn);
            _stripButtons.Remove(childId);
        }

        _children.Remove(childId);
        ChildRemoved?.Invoke(this, childId);
        UpdateStripVisibility();
        return true;
    }

    /// <summary>Returns the child with the given id; false if not found.</summary>
    public bool TryGet(string childId, out MdiChild child)
    {
        var found = _children.TryGetValue(childId, out var c);
        child = c!;
        return found;
    }

    /// <summary>Promotes the child to the top of the z-order.</summary>
    public void BringToFront(string childId)
    {
        if (!_children.TryGetValue(childId, out var child)) return;
        child.ZIndex = System.Threading.Interlocked.Increment(ref _zCounter);
    }

    /// <summary>Hides the child from the canvas and adds it to the minimised strip.</summary>
    public void Minimise(string childId)
    {
        if (!_children.TryGetValue(childId, out var child)) return;

        child.IsVisible = false;
        child.IsMinimised = true;

        if (!_stripButtons.ContainsKey(childId))
        {
            var btn = new Button
            {
                Content = child.Title,
                Tag     = childId,
                Margin  = new Thickness(2, 0),
            };
            btn.Click += (_, _) => Restore(childId);
            _stripButtons[childId] = btn;
            _minimisedStrip?.Items.Add(btn);
        }

        UpdateStripVisibility();
    }

    /// <summary>Restores a minimised child back to the canvas.</summary>
    public void Restore(string childId)
    {
        if (!_children.TryGetValue(childId, out var child)) return;

        child.IsVisible = true;
        child.IsMinimised = false;

        if (_stripButtons.TryGetValue(childId, out var btn))
        {
            _minimisedStrip?.Items.Remove(btn);
            _stripButtons.Remove(childId);
        }

        BringToFront(childId);
        UpdateStripVisibility();
    }

    /// <summary>
    /// Cycles focus to the next (or previous) MDI child in titlebar left-to-right order.
    /// </summary>
    public void FocusNext(bool reverse)
    {
        var visible = _children.Values
                                .Where(c => c.IsVisible)
                                .OrderBy(c => Canvas.GetLeft(c))
                                .ToList();

        if (visible.Count == 0) return;

        var focused = visible.FirstOrDefault(c => c.IsKeyboardFocusWithin);
        var idx = focused is null ? -1 : visible.IndexOf(focused);

        int next;
        if (reverse)
            next = idx <= 0 ? visible.Count - 1 : idx - 1;
        else
            next = (idx + 1) % visible.Count;

        visible[next].Focus();
        BringToFront(visible[next].ChildId);
    }

    // ── Template application ──────────────────────────────────────────────────

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        _childCanvas    = e.NameScope.Find<Canvas>("PART_ChildCanvas");
        _minimisedStrip = e.NameScope.Find<ItemsControl>("PART_MinimisedStrip");

        // Re-add any children that were added before the template was applied.
        if (_childCanvas is not null)
        {
            foreach (var child in _children.Values)
            {
                if (!_childCanvas.Children.Contains(child))
                    _childCanvas.Children.Add(child);
            }
        }

        UpdateStripVisibility();
    }

    // ── Size change clamping ──────────────────────────────────────────────────

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        ClampAllChildren();
    }

    private void ClampAllChildren()
    {
        if (_childCanvas is null) return;

        foreach (var child in _children.Values)
        {
            if (!child.IsVisible) continue;

            var left = Canvas.GetLeft(child);
            var top  = Canvas.GetTop(child);

            var maxLeft = Bounds.Width  - MinTitlebarVisible;
            var maxTop  = Bounds.Height - MinTitlebarVisible;

            var clampedLeft = Math.Max(MinTitlebarVisible - child.Width, Math.Min(left, maxLeft));
            var clampedTop  = Math.Max(0, Math.Min(top, maxTop));

            if (Math.Abs(clampedLeft - left) > 0.5 || Math.Abs(clampedTop - top) > 0.5)
            {
                Canvas.SetLeft(child, clampedLeft);
                Canvas.SetTop(child,  clampedTop);
            }
        }
    }

    // ── Child event handlers ──────────────────────────────────────────────────

    private void OnChildBringToFrontRequested(object? sender, RoutedEventArgs e)
    {
        if (sender is MdiChild child)
            BringToFront(child.ChildId);
    }

    private void OnChildDragRequested(object? sender, MdiChildDragEventArgs e)
    {
        if (sender is not MdiChild child || _childCanvas is null) return;

        var left = Canvas.GetLeft(child) + e.DeltaX;
        var top  = Canvas.GetTop(child)  + e.DeltaY;

        // Clamp so at least MinTitlebarVisible px of the titlebar stays inside.
        var maxLeft = Bounds.Width  - MinTitlebarVisible;
        var maxTop  = Bounds.Height - MinTitlebarVisible;
        var minLeft = MinTitlebarVisible - child.Width;

        left = Math.Max(minLeft, Math.Min(left, maxLeft));
        top  = Math.Max(0, Math.Min(top, maxTop));

        Canvas.SetLeft(child, left);
        Canvas.SetTop(child,  top);
    }

    private void OnChildResizeRequested(object? sender, MdiChildResizeEventArgs e)
    {
        if (sender is not MdiChild child || _childCanvas is null) return;

        var left   = Canvas.GetLeft(child);
        var top    = Canvas.GetTop(child);
        var width  = child.Width;
        var height = child.Height;

        if (e.Edge.HasFlag(ResizeEdge.Right))
        {
            width = Math.Max(MinWidth, width + e.DeltaX);
        }

        if (e.Edge.HasFlag(ResizeEdge.Left))
        {
            var proposed = width - e.DeltaX;
            var clamped  = Math.Max(MinWidth, proposed);
            width  = clamped;
            left  += width != clamped ? 0 : e.DeltaX; // only move origin if not clamped
        }

        if (e.Edge.HasFlag(ResizeEdge.Bottom))
        {
            height = Math.Max(MinHeight, height + e.DeltaY);
        }

        if (e.Edge.HasFlag(ResizeEdge.Top))
        {
            var proposed = height - e.DeltaY;
            var clamped  = Math.Max(MinHeight, proposed);
            height  = clamped;
            top    += height != clamped ? 0 : e.DeltaY;
        }

        child.Width  = width;
        child.Height = height;
        Canvas.SetLeft(child, left);
        Canvas.SetTop(child,  top);

        ChildGeometryChanged?.Invoke(this, new MdiChildGeometryChangedEventArgs
        {
            ChildId    = child.ChildId,
            NewBounds  = new Rect(left, top, width, height),
            IsMinimised = child.IsMinimised,
        });
    }

    private void OnChildCloseRequested(object? sender, RoutedEventArgs e)
    {
        if (sender is MdiChild child)
            Remove(child.ChildId);
    }

    private void OnChildMinimiseRequested(object? sender, RoutedEventArgs e)
    {
        if (sender is MdiChild child)
            Minimise(child.ChildId);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void UpdateStripVisibility()
    {
        if (_minimisedStrip is null) return;
        _minimisedStrip.IsVisible = _stripButtons.Count > 0;
    }
}
