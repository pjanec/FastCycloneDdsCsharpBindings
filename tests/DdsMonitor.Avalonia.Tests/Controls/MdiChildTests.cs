using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using DdsMonitor.Avalonia.Controls;
using System.Reflection;
using Xunit;

namespace DdsMonitor.Avalonia.Tests.Controls;

/// <summary>
/// Headless tests for <see cref="MdiChild"/> covering styled properties,
/// buttons, context menu, drag, resize, and Escape cancellation.
/// </summary>
public sealed class MdiChildTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a window containing an MdiChild and forces layout,
    /// then returns both the window and the child.
    /// </summary>
    private static (Window window, MdiChild child) CreateWindow(string title = "Test")
    {
        var child = new MdiChild { Title = title, Width = 400, Height = 300 };
        var window = new Window
        {
            Content = child,
            Width   = 400,  // match child so titlebar (y 0..28) is reachable by headless pointer
            Height  = 300,
        };
        window.Show();
        return (window, child);
    }

    // ── 1. IsActive updates visual class ─────────────────────────────────────

    [AvaloniaFact]
    public void IsActive_True_AddsActiveClass()
    {
        var (window, child) = CreateWindow();
        try
        {
            child.IsActive = false;
            Assert.False(child.Classes.Contains("active"));

            child.IsActive = true;
            Assert.True(child.Classes.Contains("active"), "Expected 'active' class after IsActive=true.");
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void IsActive_False_RemovesActiveClass()
    {
        var (window, child) = CreateWindow();
        try
        {
            child.IsActive = true;
            child.IsActive = false;
            Assert.False(child.Classes.Contains("active"), "Expected 'active' class removed after IsActive=false.");
        }
        finally { window.Close(); }
    }

    // ── 2. Close button raises CloseRequested ─────────────────────────────────

    [AvaloniaFact]
    public void CloseButton_Click_RaisesCloseRequested()
    {
        var (window, child) = CreateWindow();
        try
        {
            var raised = false;
            child.CloseRequested += (_, _) => raised = true;

            var closeBtn = child.GetVisualDescendants()
                                .OfType<Button>()
                                .FirstOrDefault(b => b.Name == "PART_CloseButton");
            Assert.NotNull(closeBtn);
            closeBtn!.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

            Assert.True(raised, "CloseRequested was not raised when close button clicked.");
        }
        finally { window.Close(); }
    }

    // ── 3. Minimise button raises MinimiseRequested ───────────────────────────

    [AvaloniaFact]
    public void MinimiseButton_Click_RaisesMinimiseRequested()
    {
        var (window, child) = CreateWindow();
        try
        {
            var raised = false;
            child.MinimiseRequested += (_, _) => raised = true;

            var minBtn = child.GetVisualDescendants()
                              .OfType<Button>()
                              .FirstOrDefault(b => b.Name == "PART_MinimiseButton");
            Assert.NotNull(minBtn);
            minBtn!.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

            Assert.True(raised, "MinimiseRequested was not raised when minimise button clicked.");
        }
        finally { window.Close(); }
    }

    // ── 4. Context menu has the six required items ────────────────────────────

    [AvaloniaFact]
    public void TitlebarContextMenu_HasSixMenuItems()
    {
        var (window, child) = CreateWindow();
        try
        {
            var titlebar = child.GetVisualDescendants()
                                .OfType<Border>()
                                .FirstOrDefault(b => b.Name == "PART_Titlebar");
            Assert.NotNull(titlebar);

            var menu = titlebar!.ContextMenu;
            Assert.NotNull(menu);

            // 4 dock MenuItems + 1 Minimise MenuItem + 1 Close MenuItem = 6 MenuItems
            // (the Separator between dock items and the rest is excluded by .OfType<MenuItem>())
            var menuItems = menu!.Items.OfType<MenuItem>().ToList();
            Assert.Equal(6, menuItems.Count);
        }
        finally { window.Close(); }
    }

    // ── 5. Drag delta raises DragRequested ────────────────────────────────────

    [AvaloniaFact]
    public void Drag_PointerMoved_RaisesDragRequested()
    {
        var (window, child) = CreateWindow();
        try
        {
            MdiChildDragEventArgs? dragArgs = null;
            child.DragRequested += (_, e) => dragArgs = e;

            // Child fills the 400×300 window; titlebar occupies y = 0..28.
            // Press in the middle of the titlebar, then move right to trigger DragRequested.
            window.MouseDown(new Point(200, 14), MouseButton.Left, RawInputModifiers.None);
            window.MouseMove(new Point(250, 14), RawInputModifiers.None);
            window.MouseUp(new Point(250, 14), MouseButton.Left, RawInputModifiers.None);

            Assert.NotNull(dragArgs);
        }
        finally { window.Close(); }
    }

    // ── 6. Resize BottomRight raises ResizeRequested ──────────────────────────

    [AvaloniaFact]
    public void Resize_BottomRight_RaisesResizeRequested()
    {
        var (window, child) = CreateWindow();
        try
        {
            MdiChildResizeEventArgs? resizeArgs = null;
            child.ResizeRequested += (_, e) => resizeArgs = e;

            // PART_ResizeBottomRight is a 10×10 Border at bottom-right inside the 1 px outer
            // border. In the 400×300 window its bounds are (389,289)..(399,299); centre ≈ (394,294).
            window.MouseDown(new Point(394, 294), MouseButton.Left, RawInputModifiers.None);
            window.MouseMove(new Point(410, 310), RawInputModifiers.None);
            window.MouseUp(new Point(410, 310), MouseButton.Left, RawInputModifiers.None);

            Assert.NotNull(resizeArgs);
            Assert.True(resizeArgs!.Edge.HasFlag(ResizeEdge.Bottom),
                $"Expected Bottom in resize edge {resizeArgs.Edge}.");
            Assert.True(resizeArgs!.Edge.HasFlag(ResizeEdge.Right),
                $"Expected Right in resize edge {resizeArgs.Edge}.");
        }
        finally { window.Close(); }
    }

    // ── 7. Escape during drag raises DragCancelled ────────────────────────────

    [AvaloniaFact]
    public void Escape_DuringDrag_RaisesDragCancelled()
    {
        var (window, child) = CreateWindow();
        try
        {
            var cancelled = false;
            child.DragCancelled += (_, _) => cancelled = true;

            // Escape with no drag active must NOT raise DragCancelled.
            child.Focus();
            window.KeyPress(Key.Escape, RawInputModifiers.None);
            Assert.False(cancelled, "DragCancelled should not fire when not dragging.");

            // Force drag mode via reflection (InteractionMode is a private nested enum).
            var modeField = typeof(MdiChild).GetField("_mode",
                BindingFlags.NonPublic | BindingFlags.Instance)!;
            modeField.SetValue(child, Enum.ToObject(modeField.FieldType, 1)); // 1 = Drag

            // Pressing Escape while in drag mode MUST raise DragCancelled.
            child.Focus();
            window.KeyPress(Key.Escape, RawInputModifiers.None);
            Assert.True(cancelled, "DragCancelled must fire when Escape is pressed during drag.");
        }
        finally { window.Close(); }
    }
}
