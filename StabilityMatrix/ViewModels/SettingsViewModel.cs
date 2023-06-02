using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ookii.Dialogs.Wpf;
using StabilityMatrix.Api;
using StabilityMatrix.Helper;
using StabilityMatrix.Models;
using StabilityMatrix.Python;
using Wpf.Ui.Appearance;
using Wpf.Ui.Contracts;
using Wpf.Ui.Controls.Window;
using ISnackbarService = StabilityMatrix.Helper.ISnackbarService;

namespace StabilityMatrix.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsManager settingsManager;
    private readonly IPyRunner pyRunner;
    private readonly ISnackbarService snackbarService;

    public ObservableCollection<string> AvailableThemes => new()
    {
        "Light",
        "Dark",
        "System",
    };

    public ObservableCollection<WindowBackdropType> AvailableBackdrops => new()
    {
        WindowBackdropType.Acrylic,
        WindowBackdropType.Auto,
        WindowBackdropType.Mica,
        WindowBackdropType.None,
        WindowBackdropType.Tabbed
    };
    private readonly IContentDialogService contentDialogService;
    private readonly IA3WebApi a3WebApi;

    public SettingsViewModel(ISettingsManager settingsManager, IContentDialogService contentDialogService, IA3WebApi a3WebApi, IPyRunner pyRunner, ISnackbarService snackbarService)
    {
        this.settingsManager = settingsManager;
        this.contentDialogService = contentDialogService;
        this.snackbarService = snackbarService;
        this.a3WebApi = a3WebApi;
        this.pyRunner = pyRunner;
        SelectedTheme = settingsManager.Settings.Theme ?? "Dark";
        WindowBackdropType = settingsManager.Settings.WindowBackdropType;
    }

    [ObservableProperty]
    private string selectedTheme;

    [ObservableProperty] 
    private WindowBackdropType windowBackdropType;
    
    partial void OnSelectedThemeChanged(string value)
    {
        settingsManager.SetTheme(value);
        ApplyTheme(value);
    }

    partial void OnWindowBackdropTypeChanged(WindowBackdropType oldValue, WindowBackdropType newValue)
    {
        settingsManager.SetWindowBackdropType(newValue);
        if (Application.Current.MainWindow != null)
        {
            WindowBackdrop.ApplyBackdrop(Application.Current.MainWindow, newValue);
        }
    }

    [ObservableProperty]
    private string gpuInfo =
        $"{HardwareHelper.GetGpuChipName()} ({HardwareHelper.GetGpuMemoryBytes() / 1024 / 1024 / 1024} GB)";

    [ObservableProperty] private string? gitInfo;

    [ObservableProperty] private string? testProperty;

    public AsyncRelayCommand PythonVersionCommand => new(async () =>
    {
        // Get python version
        await pyRunner.Initialize();
        var result = await pyRunner.GetVersionInfo();
        // Show dialog box
        var dialog = contentDialogService.CreateDialog();
        dialog.Title = "Python version info";
        dialog.Content = result;
        dialog.PrimaryButtonText = "Ok";
        await dialog.ShowAsync();
    });

    public RelayCommand AddInstallationCommand => new(() =>
    {
        // Show dialog box to choose a folder
        var dialog = new VistaFolderBrowserDialog
        {
            Description = "Select a folder",
            UseDescriptionForTitle = true
        };
        if (dialog.ShowDialog() != true) return;
        var path = dialog.SelectedPath;
        if (path == null) return;

        // Create package
        var package = new InstalledPackage
        {
            Id = Guid.NewGuid(),
            DisplayName = Path.GetFileName(path),
            Path = path,
            PackageName = "dank-diffusion",
            PackageVersion = "v1.0.0",
        };

        // Add package to settings
        settingsManager.AddInstalledPackage(package);
    });

    [RelayCommand]
    private async Task PingWebApi()
    {
        var result = await snackbarService.TryAsync(a3WebApi.GetPing(), "Failed to ping web api");

        if (result.IsSuccessful)
        {
            var dialog = contentDialogService.CreateDialog();
            dialog.Title = "Web API ping";
            dialog.Content = result;
            dialog.PrimaryButtonText = "Ok";
            await dialog.ShowAsync();
        }
    }
    
    [RelayCommand]
    private void OpenAppDataDirectory()
    {
        // Open app data in file explorer
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var appPath = Path.Combine(appDataPath, "StabilityMatrix");
        Process.Start("explorer.exe", appPath);
    }

    private void ApplyTheme(string value)
    {
        switch (value)
        {
            case "Light":
                Theme.Apply(ThemeType.Light, WindowBackdropType);
                break;
            case "Dark":
                Theme.Apply(ThemeType.Dark, WindowBackdropType);
                break;
            case "System":
                Theme.Apply(SystemInfo.ShouldUseDarkMode() ? ThemeType.Dark : ThemeType.Light, WindowBackdropType);
                break;
        }
    }

    public async Task OnLoaded()
    {
        SelectedTheme = string.IsNullOrWhiteSpace(settingsManager.Settings.Theme)
            ? "Dark"
            : settingsManager.Settings.Theme;
        GitInfo = await ProcessRunner.GetProcessOutputAsync("git", "--version");

        TestProperty = $"{SystemParameters.PrimaryScreenHeight} x {SystemParameters.PrimaryScreenWidth}";
    }
}
