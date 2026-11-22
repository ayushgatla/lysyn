# Publishing lysyn to the Microsoft Store

This guide outlines the exact, step-by-step process to publish **lysyn** to the Microsoft Store.

---

## 🧭 Which Publishing Method Should You Choose?

Microsoft Store supports two submission methods for Windows desktop apps:

| Feature | Method 1: Win32 Installer (Recommended 🌟) | Method 2: Packaged MSIX |
| :--- | :--- | :--- |
| **Package Format** | `lysyn-setup.exe` (Our built installer) | `.msix` / `.msixupload` |
| **Effort** | **Very Simple** (Ready immediately) | Requires MSIX Packaging Tool |
| **Installation** | Runs your installer with `/S` silent flag | Windows App Installer sandbox |
| **Filesystem Access** | Native `%LOCALAPPDATA%` and SMTC access | Virtualized AppData container |
| **Hosting** | Upload directly or link to your GitHub Release | Upload directly to Partner Center |

> **Recommendation**: Use **Method 1 (Win32 Installer)**. It is natively supported by Microsoft Store on Windows 10/11, requires zero sandboxing workarounds, and works out-of-the-box with `lysyn-setup.exe`.

---

## 📋 Step 1: Set Up Your Microsoft Partner Center Account

1. Go to the [Microsoft Partner Center](https://partner.microsoft.com/dashboard).
2. Sign in with your Microsoft account.
3. Enroll as a **Windows Developer**:
   - **Individual Account**: ~$19 USD (one-time fee, no yearly renewal).
   - **Company Account**: ~$99 USD.
4. Complete your identity verification.

---

## 🏷️ Step 2: Reserve Your App Name

1. In the Partner Center Dashboard, click **Apps & games** > **New product**.
2. Select **MSIX or PWA app** or **Windows app**.
3. Enter `lysyn` (or an alternative like `lysyn - Dynamic Lyrics Widget` if already taken).
4. Click **Check availability** and then **Reserve product name**.

---

## 🚀 Step 3: Submitting via Method 1 (Win32 / EXE Installer)

If submitting as a Win32 app:

1. **Package / Installer URL**:
   - Upload `dist\lysyn-setup.exe` directly in Partner Center, **or**
   - Provide a direct HTTPS download link (e.g., your GitHub Release asset URL: `https://github.com/YOUR_USER/lysyn/releases/download/v1.0.0/lysyn-setup.exe`).
2. **Installer Arguments**:
   - **Silent install argument**: `/S`
   - **Silent uninstall command**:
     ```powershell
     powershell -ExecutionPolicy Bypass -File "%LOCALAPPDATA%\Programs\lysyn\Uninstall-lysyn.ps1"
     ```
3. **Architecture**: Select **x64**.
4. **Minimum OS Version**: Windows 10 build `19041` (Version 2004) or higher.

---

## 📦 Step 4: Submitting via Method 2 (Packaged MSIX)

If you prefer an MSIX package:

1. Assets have been pre-generated for you in `Assets\Store\`:
   - `StoreLogo.png` (50x50)
   - `Square44x44Logo.png` (44x44)
   - `Square150x150Logo.png` (150x150)
   - `Square310x310Logo.png` (310x310)
   - `Wide310x150Logo.png` (310x150)
   - `SplashScreen.png` (620x300)
2. Open `Package.appxmanifest` and update:
   - `Publisher`: Replace `CN=YOUR-PARTNER-CENTER-ID` with your Publisher ID from Partner Center (**Account settings** > **Organization profile** > **Publisher ID**).
3. Use the free [MSIX Packaging Tool](https://apps.microsoft.com/detail/9n5lw3jbcxkf) from the Microsoft Store or `makeappx.exe` to package `dist\lysyn` into `lysyn.msix`.

---

## 🎨 Step 5: Store Listing & Metadata

In Partner Center, configure your store listing:

### Product Description
- **Title**: `lysyn - Dynamic Lyrics Island`
- **Short Description**:
  > Dynamic Island notch widget with real-time synchronized lyrics, audio visualizer, and animated companions for Windows.
- **Keywords / Search Terms**:
  `lyrics`, `dynamic island`, `music widget`, `spotify lyrics`, `youtube lyrics`, `visualizer`, `notch`, `desktop widget`
- **Category**:
  - Primary: `Music & Video` > `Music`
  - Secondary: `Utilities & Tools`

### Visual Assets Needed for the Store Page
1. **App Icon**: Use [Assets/logo.jpg](file:///c:/Users/ayush/Downloads/project/New%20folder/nodge/Assets/logo.jpg) (or PNG, minimum 512x512).
2. **Screenshots**:
   - Capture 2–4 high-resolution screenshots (1920x1080 or 1366x768):
     - Expanded notch with live synchronized lyrics and visualizer.
     - Settings window dashboard.
     - Closed compact notch at screen top.

### Age Rating
Complete the standard IARC questionnaire:
- App Type: Utility / Entertainment
- Violence / Blood: None
- Language: None
- Result: **All Ages / Everyone (PEGI 3, ESRB Everyone)**.

### Pricing & Markets
- Base Price: **Free** (or choose your preferred price tier).
- Markets: Select **All markets** (Worldwide).

---

## 🔍 Step 6: Certification & Review

1. Under **Notes for certification**, write:
   > "lysyn is a desktop dynamic island widget that displays media metadata and synchronized lyrics from active Windows SMTC audio sessions (Spotify, YouTube, browsers). It requires no special login or credentials."
2. Click **Submit to the Store**.
3. The automated review and human verification typically takes **24 to 48 hours**. Once passed, **lysyn** will be publicly live and downloadable directly from the Microsoft Store!
