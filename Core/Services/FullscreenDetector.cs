using System;
using LyricIsland.Core.Native;

namespace LyricIsland.Core.Services;

public static class FullscreenDetector
{
    public static bool IsForegroundAppFullscreen()
    {
        var foregroundHwnd = NativeMethods.GetForegroundWindow();
        if (foregroundHwnd == IntPtr.Zero) return false;

        var desktopHwnd = NativeMethods.GetDesktopWindow();
        var shellHwnd = NativeMethods.GetShellWindow();

        if (foregroundHwnd == desktopHwnd || foregroundHwnd == shellHwnd)
        {
            return false;
        }

        if (!NativeMethods.GetWindowRect(foregroundHwnd, out var appRect))
        {
            return false;
        }

        var hMonitor = NativeMethods.MonitorFromWindow(foregroundHwnd, NativeMethods.MONITOR_DEFAULTTONEAREST);
        var monitorInfo = new NativeMethods.MONITORINFO();
        monitorInfo.cbSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf(monitorInfo);

        if (!NativeMethods.GetMonitorInfo(hMonitor, ref monitorInfo))
        {
            return false;
        }

        var monitorRect = monitorInfo.rcMonitor;

        // An app is fullscreen if it covers or exceeds the monitor's bounds
        return appRect.Left <= monitorRect.Left &&
               appRect.Top <= monitorRect.Top &&
               appRect.Right >= monitorRect.Right &&
               appRect.Bottom >= monitorRect.Bottom;
    }
}
