using System;
using System.IO;
using System.Linq;
using System.Windows;

namespace lysyn.Installer;

public partial class App : Application
{
    private void Application_Startup(object sender, StartupEventArgs e)
    {
        bool isSilent = e.Args.Any(a =>
            a.Equals("/S", StringComparison.OrdinalIgnoreCase) ||
            a.Equals("/silent", StringComparison.OrdinalIgnoreCase) ||
            a.Equals("-s", StringComparison.OrdinalIgnoreCase) ||
            a.Equals("--quiet", StringComparison.OrdinalIgnoreCase));

        if (isSilent)
        {
            try
            {
                string targetDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Programs",
                    "lysyn"
                );

                InstallerEngine.ExecuteInstallation(targetDir, true, true, true, null);
                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Installation failed: {ex.Message}");
                Environment.Exit(1);
            }
            return;
        }

        var mainWindow = new MainWindow();
        mainWindow.Show();
    }
}
