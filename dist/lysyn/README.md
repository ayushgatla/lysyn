# lysyn

<p align="center">
  <img src="Assets/logo.jpg" alt="lysyn Logo" width="128" height="128" style="border-radius: 24px;" />
</p>

<p align="center">
  <strong>Dynamic Island notch widget with real-time synced lyrics, reactive visualizer, and animated companions for Windows.</strong>
</p>

<p align="center">
  <img src="https://img.shields.io/badge/.NET-9.0-blue?style=flat-square" alt=".NET 9" />
  <img src="https://img.shields.io/badge/Platform-Windows%2010%2F11-0078D6?style=flat-square" alt="Windows" />
  <img src="https://img.shields.io/badge/Theme-Zero--Gradient%20Solid-101015?style=flat-square" alt="Zero Gradient" />
  <img src="https://img.shields.io/badge/License-MIT-green?style=flat-square" alt="License" />
</p>

---

## 🌟 Overview

**lysyn** is a lightweight, distraction-free desktop notch application inspired by modern dynamic islands. Sitting discreetly at the top edge of your monitor, it automatically detects any media playing across Windows (Spotify, YouTube, YouTube Music, Apple Music, browsers, and media players) to deliver synchronized, word-accurate lyrics, playback controls, an audio visualizer ring, and animated companions.

---

## ✨ Features

- 🎵 **Real-time Synchronized Lyrics**: Powered by LRCLIB with offline caching and smart title normalization (automatically strips `Official Video`, `VEVO`, `Topic`, `Live`, `Extended`, etc., to locate lyrics effortlessly).
- ⚪ **Clean Solid Theming (Zero Gradients)**: High-contrast, pure solid color system. Defaults to a crisp **White Theme** (`#F4F4F5` / `#FFFFFF`) with 9 solid presets (Red, Orange, Yellow, Green, Cyan, Blue, Purple, Pink, White) and a fine-tune Hue slider.
- 🔤 **Google Sans Typography**: Clean, modern typography out of the box with custom font picker and adjustable font sizes.
- 💫 **Audio-Reactive Circular Visualizer**: A pulsing circular visualizer that wraps your media album art, synchronized strictly to your chosen theme accent.
- 🎀 **Animated Character Companions**: A customizable animated GIF companion on the right edge of the notch (defaults to **Chika**, with included Arisu, Cat, Bunny presets, horizontal flipping, and custom file support).
- 🖥️ **Adaptive Dynamic Island**:
  - Compact idle state pinned cleanly to the top screen edge.
  - Expands smoothly on mouse hover to reveal full playback controls, scrubber bar, and lyrics.
  - Pin mode to keep the island expanded permanently.
- ⚙️ **Modern Dashboard Settings**: Tabbed control panel matching modern widescreen layouts that live-updates with your active theme color.
- ⚡ **Lightweight & High Performance**: Sub-25ms synchronization loop with active memory trimming and native Windows SMTC media integration.

---

## 🚀 Installation

### Option 1: Executable Installer (Recommended)
1. Download or open `dist/lysyn-setup.exe`.
2. Double-click the installer.
3. Choose your shortcut preferences and click **Install lysyn**.
4. The installer automatically extracts files to `%LOCALAPPDATA%\Programs\lysyn`, registers Start Menu & Desktop shortcuts, and launches the app.

> **Silent Installation**: Run `lysyn-setup.exe /S` for unattended deployment.

### Option 2: PowerShell Installer
Run the included installation script in PowerShell:
```powershell
powershell -ExecutionPolicy Bypass -File .\Install-lysyn.ps1
```

### Option 3: Portable Archive
Extract `dist/lysyn-v1.0.0-windows-x64.zip` anywhere and launch `lysyn.exe`.

---

## ⌨️ Global Shortcuts & Controls

| Shortcut / Action | Function |
| :--- | :--- |
| `Ctrl + Alt + L` | Toggle notch visibility on/off |
| `Ctrl + Alt + C` | Toggle click-through mode (transparent to mouse clicks) |
| **Hover Notch** | Expands the island into media player view |
| **Right-Click Notch** | Opens context menu (Settings, Resync Media, Exit) |
| **Click Play/Pause** | Toggles playback in active media app |
| **Click Track Bar** | Scrubs audio timeline directly |

---

## 🛠️ Configuration & Settings

Access settings anytime by right-clicking the island or system tray icon and selecting **Settings**:

- **Theme & Colors**:
  - 9 solid presets: Red, Orange, Yellow, Green, Cyan, Blue, Purple, Pink, White.
  - 0–360° Hue slider with zero gradients.
  - Live preview reflects color choices instantaneously.
- **GIF Companion**:
  - Presets: Chika (Default), Arisu, Cat, Bunny.
  - Browse local `.gif`, `.webp`, or `.png` files.
  - Speed modifier (0.25x to 3.0x) and horizontal mirror toggle.
- **Notch & Display**:
  - Configurable width (240–600px) and height (32–90px).
  - Recenter button to re-align notch to the top-center of any display.
  - Auto-expand on hover and permanent pin toggles.
- **Lyrics & Timing**:
  - Font selection (defaults to `Google Sans`) and font sizing.
  - Manual sync offset adjustment (±50ms steps, up to ±3000ms).
  - One-click lyrics cache purging.
- **Integration**:
  - Always on top, click-through mode, and auto-hide during full-screen games/apps.
  - Run automatically when Windows boots.
  - Customizable hotkeys.

---

## 🏪 Microsoft Store Publishing

Looking to publish **lysyn** to the official Microsoft Store? Follow the step-by-step instructions in the [Microsoft Store Guide](file:///c:/Users/ayush/Downloads/project/New%20folder/nodge/MICROSOFT_STORE_GUIDE.md). All required visual tile assets and the `Package.appxmanifest` template are already generated in `Assets/Store/`.

---

## 🏗️ Building from Source

### Prerequisites
- Windows 10/11 (64-bit)
- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)

### Build & Package
Open PowerShell in the project directory:
```powershell
# 1. Run unit tests
dotnet test

# 2. Package release and build standalone installer
.\build-package.ps1
```

The output will be generated inside `dist/`:
- `dist/lysyn-setup.exe` (Standalone Windows Executable Installer)
- `dist/lysyn-v1.0.0-windows-x64.zip` (Portable ZIP)
- `dist/lysyn/` (Extracted release binaries)

---

## 📄 License

Distributed under the MIT License. See `LICENSE` for more information.
