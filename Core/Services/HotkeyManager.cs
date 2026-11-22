using System;
using System.Windows.Input;
using System.Windows.Interop;
using LyricIsland.Core.Native;

namespace LyricIsland.Core.Services;

public class HotkeyManager : IDisposable
{
    private IntPtr _windowHandle;
    private HwndSource? _source;
    private const int ToggleVisibilityHotkeyId = 9001;
    private const int ToggleClickThroughHotkeyId = 9002;

    public event Action? ToggleVisibilityRequested;
    public event Action? ToggleClickThroughRequested;

    public void Register(IntPtr hWnd, string toggleHotkeyStr, string clickThroughHotkeyStr)
    {
        _windowHandle = hWnd;
        _source = HwndSource.FromHwnd(hWnd);
        _source?.AddHook(HwndHook);

        RegisterParsedHotkey(ToggleVisibilityHotkeyId, toggleHotkeyStr);
        RegisterParsedHotkey(ToggleClickThroughHotkeyId, clickThroughHotkeyStr);
    }

    private void RegisterParsedHotkey(int id, string hotkeyStr)
    {
        if (string.IsNullOrWhiteSpace(hotkeyStr)) return;

        uint modifiers = NativeMethods.MOD_NOREPEAT;
        Key key = Key.None;

        var parts = hotkeyStr.Split('+');
        foreach (var p in parts)
        {
            var part = p.Trim().ToLowerInvariant();
            if (part == "ctrl" || part == "control") modifiers |= NativeMethods.MOD_CONTROL;
            else if (part == "alt") modifiers |= NativeMethods.MOD_ALT;
            else if (part == "shift") modifiers |= NativeMethods.MOD_SHIFT;
            else if (part == "win" || part == "windows") modifiers |= NativeMethods.MOD_WIN;
            else if (Enum.TryParse<Key>(p.Trim(), true, out var parsedKey))
            {
                key = parsedKey;
            }
        }

        if (key != Key.None)
        {
            int virtualKey = KeyInterop.VirtualKeyFromKey(key);
            NativeMethods.RegisterHotKey(_windowHandle, id, modifiers, (uint)virtualKey);
        }
    }

    private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == NativeMethods.WM_HOTKEY)
        {
            int id = wParam.ToInt32();
            if (id == ToggleVisibilityHotkeyId)
            {
                ToggleVisibilityRequested?.Invoke();
                handled = true;
            }
            else if (id == ToggleClickThroughHotkeyId)
            {
                ToggleClickThroughRequested?.Invoke();
                handled = true;
            }
        }

        return IntPtr.Zero;
    }

    public void Dispose()
    {
        if (_windowHandle != IntPtr.Zero)
        {
            NativeMethods.UnregisterHotKey(_windowHandle, ToggleVisibilityHotkeyId);
            NativeMethods.UnregisterHotKey(_windowHandle, ToggleClickThroughHotkeyId);
            _source?.RemoveHook(HwndHook);
            _windowHandle = IntPtr.Zero;
        }
    }
}
