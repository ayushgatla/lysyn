# LyricIsland – Windows Dynamic Lyrics Widget

<p align="center">
  <strong>A lightweight, native Windows desktop application inspired by Caelestia aesthetics and Dynamic Island design.</strong><br>
  Floats unobtrusively on your desktop, displaying synchronized real-time lyrics flanked by animated companion characters.
</p>

---

## ✨ Features

- **Dynamic Island Aesthetic**: Frosted glass / Acrylic styling, rounded pill shape, subtle neon purple aura glow, and smooth line transitions.
- **Universal Music Detection**: Integrates with Windows Media Control (`GlobalSystemMediaTransportControlsSessionManager`), detecting playback seamlessly from **Spotify**, **YouTube Music** (Chrome/Edge/Brave), **Apple Music**, **TIDAL**, **foobar2000**, and any compatible media player without needing plugins or browser extensions.
- **Synchronized Lyrics via LRCLIB**: Real-time timestamped lyrics synchronization, highlighting current line with previous and upcoming line preview, sub-second interpolation, and intelligent offline disk caching.
- **Animated Companions**: Dual left and right animated GIF characters that react dynamically to your playback status (`Idle`, `Playing`, `Paused`, and `SongChanged` transitions).
- **Native Windows Integration**:
  - Always-on-top floating mode.
  - Drag anywhere to reposition with automatic screen edge snapping.
  - Click-through toggle mode (`WS_EX_TRANSPARENT`) for seamless background overlay.
  - Global hotkeys (`Ctrl+Alt+L` to toggle visibility, `Ctrl+Alt+C` to toggle click-through).
  - Native Win32 System Tray icon with quick actions and context menu.
  - Multi-monitor support and DPI scaling awareness.
  - Fullscreen auto-hide (hides during games and full-screen presentations).
  - Start with Windows support.
- **Ultra-Lightweight & Efficient**:
  - **~63 MB idle RAM footprint** (well below the 100 MB budget).
  - Zero unnecessary background polling or CPU cycles.
  - Animations pause automatically when media is stopped or window is hidden.

---

## 🚀 Quick Start

### Running from Published Release
Run `LyricIsland.exe` located in the `bin\Publish` or extract `dist\LyricIsland-v1.0.0-windows-x64.zip`.

### Installing into System
Right-click `Install-LyricIsland.ps1` and select **Run with PowerShell** to install LyricIsland to `%LocalAppData%\Programs\LyricIsland` and create Start Menu and Desktop shortcuts.

### Building from Source
Requirements: .NET 8 or .NET 9 SDK (Windows Desktop).

```powershell
# Restore and build solution
dotnet build

# Run unit tests
dotnet test Tests\LyricIsland.Tests.csproj

# Publish release package
dotnet publish LyricIsland.csproj -c Release -o bin\Publish
```

---

## ⌨ Keyboard Shortcuts

| Shortcut | Action |
|---|---|
| `Ctrl + Alt + L` | Toggle widget visibility (Show / Hide) |
| `Ctrl + Alt + C` | Toggle Click-Through mode |
| `Right Click` (on Widget or Tray) | Open Context Menu (Settings, Lock, Refresh, Exit) |

---

## ⚙ Customization & Settings

Open the Settings window from the system tray menu or by right-clicking the island:
- **Appearance**: Adjust widget width (480–960px), height, opacity, font family, main/sub font sizes, and toggle progress indicator.
- **Lyrics & Timing**: Configure manual timing offset (+/- ms) to align lyrics perfectly with specific audio delays, and manage the local cache.
- **Characters**: Customize left and right GIF animations independently for Idle, Playing, and Paused states, adjust animation speed, and import custom GIFs.
- **Integration**: Toggle Always-on-Top, Click-Through, Auto-Hide in Fullscreen, and Start with Windows.

---

## 🏛 Architecture

LyricIsland is built with a strictly decoupled, modular architecture:
- `LyricIsland.Core.Models`: Data contracts for media sessions, lyrics, and configuration.
- `LyricIsland.Core.Native`: P/Invoke Win32 interop (`user32.dll`, `dwmapi.dll`, `shell32.dll`, `psapi.dll`).
- `LyricIsland.Core.Services`:
  - `MediaSessionManager`: Interfaces with Windows SMTC.
  - `LyricsService`: LRCLIB REST client and persistent file cache.
  - `LyricsSynchronizer`: High-resolution stopwatch interpolation engine.
  - `AnimationManager`: Companion character state machine.
  - `SettingsManager`: JSON persistence in `%AppData%\LyricIsland\settings.json`.
  - `FullscreenDetector`: Native monitor and window bounds detector.
  - `HotkeyManager`: Win32 `RegisterHotKey` listener.
- `LyricIsland.UI.Controls`:
  - `GifPlayer`: Low-overhead frame decoder that halts rendering when inactive.
- `LyricIsland.UI.Views`:
  - `OverlayWindow`: Caelestia frosted glass dynamic island widget.
  - `SettingsWindow`: Tabbed settings and customization interface.
- `LyricIsland.UI.Tray`:
  - `SystemTrayManager`: Native Win32 notification icon and context menu.
