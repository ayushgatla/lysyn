using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using LyricIsland.Core.Models;
using LyricIsland.Core.Native;
using LyricIsland.Core.Services;

namespace LyricIsland.UI.Tray;

public class SystemTrayManager : IDisposable
{
    private readonly SettingsManager _settingsManager;
    private IntPtr _hwnd = IntPtr.Zero;
    private HwndSource? _hwndSource;
    private IntPtr _hIcon = IntPtr.Zero;
    private bool _isAdded;
    private const int TrayIconId = 1001;
    private const int WM_USER = 0x0400;
    private const int WM_TRAYICON = WM_USER + 101;
    private const int WM_LBUTTONUP = 0x0202;
    private const int WM_LBUTTONDBLCLK = 0x0203;
    private const int WM_RBUTTONUP = 0x0205;

    public event Action? ToggleVisibilityRequested;
    public event Action? SettingsRequested;
    public event Action? RefreshRequested;
    public event Action? ExitRequested;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NOTIFYICONDATA
    {
        public int cbSize;
        public IntPtr hWnd;
        public int uID;
        public int uFlags;
        public int uCallbackMessage;
        public IntPtr hIcon;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string szTip;
        public int dwState;
        public int dwStateMask;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string szInfo;
        public int uTimeoutOrVersion;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string szInfoTitle;
        public int dwInfoFlags;
        public Guid guidItem;
        public IntPtr hBalloonIcon;
    }

    private const int NIM_ADD = 0x00000000;
    private const int NIM_MODIFY = 0x00000001;
    private const int NIM_DELETE = 0x00000002;
    private const int NIF_MESSAGE = 0x00000001;
    private const int NIF_ICON = 0x00000002;
    private const int NIF_TIP = 0x00000004;

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern bool Shell_NotifyIconW(int dwMessage, ref NOTIFYICONDATA lpData);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr hIcon);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr LoadIcon(IntPtr hInstance, IntPtr lpIconName);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr LoadImageW(IntPtr hInst, string lpszName, uint uType, int cxDesired, int cyDesired, uint fuLoad);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr CreateIcon(IntPtr hInstance, int nWidth, int nHeight, byte cPlanes, byte cBitsPixel, byte[] lpbANDbits, byte[] lpbXORbits);

    public SystemTrayManager(SettingsManager settingsManager)
    {
        _settingsManager = settingsManager;
    }

    public void Initialize(IntPtr ownerHwnd)
    {
        _hwnd = ownerHwnd;
        _hwndSource = HwndSource.FromHwnd(_hwnd);
        _hwndSource?.AddHook(HwndHook);

        _hIcon = LoadAppIconHandle();

        var nid = new NOTIFYICONDATA
        {
            cbSize = Marshal.SizeOf<NOTIFYICONDATA>(),
            hWnd = _hwnd,
            uID = TrayIconId,
            uFlags = NIF_MESSAGE | NIF_ICON | NIF_TIP,
            uCallbackMessage = WM_TRAYICON,
            hIcon = _hIcon,
            szTip = "lysyn – Dynamic Lyrics Widget"
        };

        _isAdded = Shell_NotifyIconW(NIM_ADD, ref nid);
    }

    private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_TRAYICON && wParam.ToInt32() == TrayIconId)
        {
            int mouseMsg = lParam.ToInt32();
            if (mouseMsg == WM_LBUTTONUP || mouseMsg == WM_LBUTTONDBLCLK)
            {
                ToggleVisibilityRequested?.Invoke();
                handled = true;
            }
            else if (mouseMsg == WM_RBUTTONUP)
            {
                ShowContextMenu();
                handled = true;
            }
        }
        return IntPtr.Zero;
    }

    private void ShowContextMenu()
    {
        var menu = new ContextMenu();

        var toggleItem = new MenuItem { Header = "👁 Show / Hide Widget" };
        toggleItem.Click += (_, _) => ToggleVisibilityRequested?.Invoke();
        menu.Items.Add(toggleItem);

        menu.Items.Add(new Separator());

        var settingsItem = new MenuItem { Header = "⚙ Settings..." };
        settingsItem.Click += (_, _) => SettingsRequested?.Invoke();
        menu.Items.Add(settingsItem);

        var lockItem = new MenuItem
        {
            Header = _settingsManager.CurrentSettings.LockedPosition ? "🔓 Unlock Position" : "🔒 Lock Position"
        };
        lockItem.Click += (_, _) =>
        {
            _settingsManager.UpdateSettings(s => s.LockedPosition = !s.LockedPosition);
        };
        menu.Items.Add(lockItem);

        var clickItem = new MenuItem
        {
            Header = _settingsManager.CurrentSettings.ClickThrough ? "✓ Click-Through Enabled" : "Enable Click-Through"
        };
        clickItem.Click += (_, _) =>
        {
            _settingsManager.UpdateSettings(s => s.ClickThrough = !s.ClickThrough);
        };
        menu.Items.Add(clickItem);

        var refreshItem = new MenuItem { Header = "🔄 Resync / Refresh Media" };
        refreshItem.Click += (_, _) => RefreshRequested?.Invoke();
        menu.Items.Add(refreshItem);

        menu.Items.Add(new Separator());

        var exitItem = new MenuItem { Header = "❌ Exit lysyn" };
        exitItem.Click += (_, _) => ExitRequested?.Invoke();
        menu.Items.Add(exitItem);

        menu.IsOpen = true;
    }

    private static IntPtr LoadAppIconHandle()
    {
        try
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var candidates = new[]
            {
                Path.Combine(baseDir, "Assets", "logo.ico"),
                Path.Combine(Directory.GetCurrentDirectory(), "Assets", "logo.ico"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "lysyn", "Assets", "logo.ico")
            };

            foreach (var path in candidates)
            {
                if (File.Exists(path))
                {
                    IntPtr handle = LoadImageW(IntPtr.Zero, path, 1 /*IMAGE_ICON*/, 16, 16, 0x0010 /*LR_LOADFROMFILE*/);
                    if (handle != IntPtr.Zero)
                    {
                        return handle;
                    }
                }
            }
        }
        catch { }

        return CreateProceduralIconHandle();
    }

    private static IntPtr CreateProceduralIconHandle()
    {
        // 16x16 icon mask and color bytes
        const int size = 16;
        var andMask = new byte[size * size / 8]; // 32 bytes
        var xorMask = new byte[size * size * 4]; // 32-bit color

        // Transparent background
        for (int i = 0; i < andMask.Length; i++) andMask[i] = 0x00;

        // Draw a cute purple music circle in XOR (BGRA format)
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int idx = (y * size + x) * 4;
                double dist = Math.Sqrt((x - 7.5) * (x - 7.5) + (y - 7.5) * (y - 7.5));
                if (dist <= 7.0)
                {
                    // Purple gradient: #A855F7
                    xorMask[idx + 0] = 0xF7; // Blue
                    xorMask[idx + 1] = 0x55; // Green
                    xorMask[idx + 2] = 0xA8; // Red
                    xorMask[idx + 3] = 0xFF; // Alpha
                }
                else
                {
                    xorMask[idx + 3] = 0x00; // Transparent
                }
            }
        }

        return CreateIcon(IntPtr.Zero, size, size, 1, 32, andMask, xorMask);
    }

    public void Dispose()
    {
        if (_isAdded && _hwnd != IntPtr.Zero)
        {
            var nid = new NOTIFYICONDATA
            {
                cbSize = Marshal.SizeOf<NOTIFYICONDATA>(),
                hWnd = _hwnd,
                uID = TrayIconId
            };
            Shell_NotifyIconW(NIM_DELETE, ref nid);
            _isAdded = false;
        }

        if (_hIcon != IntPtr.Zero)
        {
            DestroyIcon(_hIcon);
            _hIcon = IntPtr.Zero;
        }

        _hwndSource?.RemoveHook(HwndHook);
    }
}
