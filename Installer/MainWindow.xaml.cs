using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace lysyn.Installer;

public partial class MainWindow : Window
{
    private readonly string _targetDir;
    private bool _isInstalling = false;
    private bool _isCompleted = false;

    public MainWindow()
    {
        InitializeComponent();

        _targetDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Programs",
            "lysyn"
        );

        TargetPathBox.Text = _targetDir;
        LoadLogo();
    }

    private void LoadLogo()
    {
        try
        {
            // Try loading from embedded resource or relative path
            string localIco = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "logo.ico");
            if (File.Exists(localIco))
            {
                HeaderLogo.Source = new BitmapImage(new Uri(localIco));
            }
        }
        catch { }
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private async void InstallBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_isCompleted)
        {
            Close();
            return;
        }

        if (_isInstalling) return;

        _isInstalling = true;
        InstallBtn.IsEnabled = false;
        CancelBtn.IsEnabled = false;
        InstallProgressBar.Visibility = Visibility.Visible;

        bool desktop = DesktopShortcutCheck.IsChecked == true;
        bool startMenu = StartMenuCheck.IsChecked == true;
        bool launch = LaunchAfterCheck.IsChecked == true;

        try
        {
            await Task.Run(() =>
            {
                InstallerEngine.ExecuteInstallation(_targetDir, desktop, startMenu, launch, (status, progress) =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        StatusText.Text = status;
                        InstallProgressBar.Value = progress;
                    });
                });
            });

            _isCompleted = true;
            StatusText.Text = "Installation completed successfully!";
            InstallProgressBar.Value = 100;
            InstallBtn.Content = "Finish";
            InstallBtn.IsEnabled = true;
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Error: {ex.Message}";
            InstallBtn.IsEnabled = true;
            CancelBtn.IsEnabled = true;
        }
        finally
        {
            _isInstalling = false;
        }
    }
}
