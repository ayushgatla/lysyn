using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace lysyn.Installer;

public static class InstallerEngine
{
    public static void ExecuteInstallation(
        string targetDir,
        bool createDesktop,
        bool createStartMenu,
        bool launch,
        Action<string, int>? progressCallback)
    {
        // 1. Stop any running instances
        progressCallback?.Invoke("Stopping running instances of lysyn...", 15);
        StopRunningProcesses();
        Thread.Sleep(500);

        // 2. Prepare destination directory
        progressCallback?.Invoke("Preparing target folder...", 30);
        if (!Directory.Exists(targetDir))
        {
            Directory.CreateDirectory(targetDir);
        }

        // 3. Extract payload zip
        progressCallback?.Invoke("Extracting application binaries and assets...", 55);
        ExtractPayload(targetDir);

        // 4. Create Shortcuts
        string exePath = Path.Combine(targetDir, "lysyn.exe");
        string iconPath = Path.Combine(targetDir, "Assets", "logo.ico");

        if (createStartMenu)
        {
            progressCallback?.Invoke("Creating Start Menu shortcut...", 75);
            try
            {
                string startMenuDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "Microsoft", "Windows", "Start Menu", "Programs"
                );
                string shortcutPath = Path.Combine(startMenuDir, "lysyn.lnk");
                CreateShortcut(shortcutPath, exePath, targetDir, iconPath, "lysyn – Windows Dynamic Lyrics Widget");
            }
            catch { }
        }

        if (createDesktop)
        {
            progressCallback?.Invoke("Creating Desktop shortcut...", 85);
            try
            {
                string desktopDir = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                string shortcutPath = Path.Combine(desktopDir, "lysyn.lnk");
                CreateShortcut(shortcutPath, exePath, targetDir, iconPath, "lysyn – Windows Dynamic Lyrics Widget");
            }
            catch { }
        }

        // 5. Launch if requested
        if (launch && File.Exists(exePath))
        {
            progressCallback?.Invoke("Launching lysyn...", 95);
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = exePath,
                    WorkingDirectory = targetDir,
                    UseShellExecute = true
                });
            }
            catch { }
        }

        progressCallback?.Invoke("Installation completed!", 100);
    }

    private static void StopRunningProcesses()
    {
        foreach (var name in new[] { "lysyn", "LyricIsland" })
        {
            foreach (var proc in Process.GetProcessesByName(name))
            {
                try
                {
                    proc.Kill();
                    proc.WaitForExit(1500);
                }
                catch { }
            }
        }
    }

    private static void ExtractPayload(string targetDir)
    {
        var asm = Assembly.GetExecutingAssembly();
        var resourceName = asm.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith(".zip", StringComparison.OrdinalIgnoreCase));

        Stream? zipStream = null;
        if (!string.IsNullOrEmpty(resourceName))
        {
            zipStream = asm.GetManifestResourceStream(resourceName);
        }

        if (zipStream == null)
        {
            // Fallback: check file next to setup.exe or in same directory
            var localZip = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "payload.zip");
            if (File.Exists(localZip))
            {
                zipStream = File.OpenRead(localZip);
            }
        }

        if (zipStream != null)
        {
            using (zipStream)
            using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Read))
            {
                foreach (var entry in archive.Entries)
                {
                    if (string.IsNullOrEmpty(entry.Name))
                    {
                        // Directory entry
                        string dirPath = Path.Combine(targetDir, entry.FullName);
                        Directory.CreateDirectory(dirPath);
                        continue;
                    }

                    string destinationPath = Path.Combine(targetDir, entry.FullName);
                    string? dir = Path.GetDirectoryName(destinationPath);
                    if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }

                    entry.ExtractToFile(destinationPath, overwrite: true);
                }
            }
            return;
        }

        throw new FileNotFoundException("Embedded installation payload not found inside installer.");
    }

    private static void CreateShortcut(string shortcutPath, string targetPath, string workingDir, string iconPath, string description)
    {
        Type? shellType = Type.GetTypeFromProgID("WScript.Shell");
        if (shellType == null) return;

        dynamic? shell = Activator.CreateInstance(shellType);
        if (shell == null) return;

        dynamic shortcut = shell.CreateShortcut(shortcutPath);
        shortcut.TargetPath = targetPath;
        shortcut.WorkingDirectory = workingDir;
        if (File.Exists(iconPath))
        {
            shortcut.IconLocation = iconPath + ",0";
        }
        shortcut.Description = description;
        shortcut.Save();
    }
}
