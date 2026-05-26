using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using DdsMonitor.Avalonia.Controls;
using Xunit;

namespace DdsMonitor.Avalonia.Tests.Controls;

/// <summary>
/// Headless tests for <see cref="MdiHost"/> covering child management,
/// z-order, drag clamping, resize min-size enforcement, minimise/restore,
/// focus cycling, and no-op removal.
/// </summary>
public sealed class MdiHostTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>Creates and shows a window containing an MdiHost.</summary>
    private static (Window window, MdiHost host) CreateWindow()
    {
        var host = new MdiHost { Width = 800, Height = 600 };
        var window = new Window
        {
            Content = host,
            Width   = 900,
            Height  = 700,
        };
        window.Show();
        return (window, host);
    }

    private static MdiChild MakeChild(string id, string title = "Child")
        => new MdiChild { ChildId = id, Title = title };

    // ── 1. Adding three children ──────────────────────────────────────────────

    [AvaloniaFact]
    public void Add_ThreeChildren_CountIsThree()
    {
        var (window, host) = CreateWindow();
        try
        {
            host.Add(MakeChild("a", "A"), 10, 10, 300, 200);
            host.Add(MakeChild("b", "B"), 50, 50, 300, 200);
            host.Add(MakeChild("c", "C"), 90, 90, 300, 200);

            Assert.Equal(3, host.Children.Count);
        }
        finally { window.Close(); }
    }

    // ── 2. BringToFront raises ZIndex above siblings ──────────────────────────

    [AvaloniaFact]
    public void BringToFront_RaisesZIndexAboveSiblings()
    {
        var (window, host) = CreateWindow();
        try
        {
            host.Add(MakeChild("a"), 10, 10, 300, 200);
            host.Add(MakeChild("b"), 50, 50, 300, 200);
            host.Add(MakeChild("c"), 90, 90, 300, 200);

            // Bring 'a' to front.
            host.BringToFront("a");

            host.TryGet("a", out var childA);
            host.TryGet("b", out var childB);
            host.TryGet("c", out var childC);

            var zA = childA.ZIndex;
            var zB = childB.ZIndex;
            var zC = childC.ZIndex;

            Assert.True(zA > zB, $"ZIndex of 'a' ({zA}) should be > ZIndex of 'b' ({zB}) after BringToFront.");
            Assert.True(zA > zC, $"ZIndex of 'a' ({zA}) should be > ZIndex of 'c' ({zC}) after BringToFront.");
        }
        finally { window.Close(); }
    }

    // ── 3. Drag past right edge: clamp keeps ≥ 40 px inside ──────────────────

    [AvaloniaFact]
    public void Drag_PastRightEdge_ClampsPosition()
    {
        var (window, host) = CreateWindow();
        try
        {
            var child = MakeChild("x");
            host.Add(child, 10, 10, 300, 200);

            // Simulate a drag that would move the child far to the right (beyond bounds).
            var dragArgs = new MdiChildDragEventArgs(MdiChild.DragRequestedEvent)
            {
                DeltaX = 10000,
                DeltaY = 0,
            };
            // Send the event via the child control itself.
            child.RaiseEvent(dragArgs);

            var left = Canvas.GetLeft(child);
            var hostWidth = host.Bounds.Width > 0 ? host.Bounds.Width : 800;
            // At least MinTitlebarVisible (40 px) of titlebar must remain inside.
            Assert.True(left <= hostWidth - 40,
                $"Child Canvas.Left ({left}) should be ≤ host width ({hostWidth}) minus 40.");
        }
        finally { window.Close(); }
    }

    // ── 4. Resize below min: width clamps to 220 ─────────────────────────────

    [AvaloniaFact]
    public void Resize_BelowMin_ClampsToMinWidth()
    {
        var (window, host) = CreateWindow();
        try
        {
            var child = MakeChild("y");
            host.Add(child, 10, 10, 300, 200);

            // Simulate a resize that shrinks width far below minimum.
            var resizeArgs = new MdiChildResizeEventArgs(MdiChild.ResizeRequestedEvent)
            {
                Edge   = ResizeEdge.Right,
                DeltaX = -10000,
                DeltaY = 0,
            };
            child.RaiseEvent(resizeArgs);

            Assert.True(child.Width >= 220,
                $"Child width ({child.Width}) should be >= 220 after attempted resize below minimum.");
        }
        finally { window.Close(); }
    }

    // ── 5. Minimise: child invisible, strip has one item ─────────────────────

    [AvaloniaFact]
    public void Minimise_HidesChild_AndAddsToStrip()
    {
        var (window, host) = CreateWindow();
        try
        {
            host.Add(MakeChild("m", "MyPanel"), 10, 10, 300, 200);
            host.Minimise("m");

            host.TryGet("m", out var child);
            Assert.False(child.IsVisible, "Child should be invisible after Minimise.");
            Assert.True(child.IsMinimised, "Child.IsMinimised should be true after Minimise.");
            var strip = host.GetVisualDescendants()
                            .OfType<ItemsControl>()
                            .FirstOrDefault(c => c.Name == "PART_MinimisedStrip");
            Assert.NotNull(strip);
            Assert.Equal(1, strip!.Items.Count);        }
        finally { window.Close(); }
    }

    // ── 6. Restore from strip: child reappears, strip empty ──────────────────

    [AvaloniaFact]
    public void Restore_ReappearsAndStripIsEmpty()
    {
        var (window, host) = CreateWindow();
        try
        {
            host.Add(MakeChild("r", "RestoreMe"), 10, 10, 300, 200);
            host.Minimise("r");
            host.Restore("r");

            host.TryGet("r", out var child);
            Assert.True(child.IsVisible,   "Child should be visible after Restore.");
            Assert.False(child.IsMinimised, "Child.IsMinimised should be false after Restore.");

            var strip = host.GetVisualDescendants()
                            .OfType<ItemsControl>()
                            .FirstOrDefault(c => c.Name == "PART_MinimisedStrip");
            Assert.NotNull(strip);
            Assert.Equal(0, strip!.Items.Count);
        }
        finally { window.Close(); }
    }

    // ── 7. FocusNext wraps around ─────────────────────────────────────────────

    [AvaloniaFact]
    public void FocusNext_WithThreeChildren_DoesNotThrow()
    {
        var (window, host) = CreateWindow();
        try
        {
            host.Add(MakeChild("f1", "F1"),  10, 10, 300, 200);
            host.Add(MakeChild("f2", "F2"),  50, 10, 300, 200);
            host.Add(MakeChild("f3", "F3"), 100, 10, 300, 200);

            host.TryGet("f1", out var c1);
            host.TryGet("f2", out var c2);
            host.TryGet("f3", out var c3);

            // Returns the id of whichever child currently has the highest ZIndex (= at front).
            string FrontId() =>
                c1.ZIndex >= c2.ZIndex && c1.ZIndex >= c3.ZIndex ? "f1" :
                c2.ZIndex >= c1.ZIndex && c2.ZIndex >= c3.ZIndex ? "f2" :
                "f3";

            var frontBefore = FrontId(); // f3 was added last, so it starts at front

            // One forward step must bring a different child to the front.
            host.FocusNext(false);
            var frontAfter = FrontId();

            Assert.NotEqual(frontBefore, frontAfter);
        }
        finally { window.Close(); }
    }

    // ── 8. Remove("missing") returns false and is a no-op ────────────────────

    [AvaloniaFact]
    public void Remove_MissingId_ReturnsFalseAndNoOp()
    {
        var (window, host) = CreateWindow();
        try
        {
            host.Add(MakeChild("keep"), 10, 10, 300, 200);

            var result = host.Remove("does-not-exist");

            Assert.False(result, "Remove should return false for an unknown child id.");
            Assert.Equal(1, host.Children.Count);
        }
        finally { window.Close(); }
    }
}
