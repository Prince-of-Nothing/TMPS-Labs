using Microsoft.Maui.Storage;

namespace Lab3;

public partial class App : Application
{
    private AppServices? _services;

    public App()
    {
        InitializeComponent();
        UserAppTheme = Preferences.Default.Get("theme", "System") switch
        {
            "Light" => AppTheme.Light,
            "Dark" => AppTheme.Dark,
            _ => AppTheme.Unspecified,
        };
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var overridePath = Environment.GetEnvironmentVariable("TMPS_LAB3_LIBRARY");
        if (!string.IsNullOrWhiteSpace(overridePath) && StorageLocations.CanUse(overridePath))
        {
            _services ??= AppServices.Create(overridePath);
            return MainWindow(new MainPage(_services));
        }

        var saved = Preferences.Default.Get(StorageLocations.PreferenceKey, string.Empty);
        if (!string.IsNullOrWhiteSpace(saved) && StorageLocations.CanUse(saved))
        {
            _services ??= AppServices.Create(saved);
            return MainWindow(new MainPage(_services));
        }

        var setup = new StorageSetupPage();
        setup.Chosen += OnStorageChosen;
        return MainWindow(setup);
    }

    private void OnStorageChosen(string root)
    {
        Preferences.Default.Set(StorageLocations.PreferenceKey, root);
        _services = AppServices.Create(root);
        var window = Windows.FirstOrDefault();
        if (window is not null)
        {
            window.Page = new NavigationPage(new MainPage(_services))
            {
                BarBackgroundColor = Color.FromArgb("#1A1525"),
                BarTextColor = Colors.White,
            };
        }
    }

    private static Window MainWindow(Page page) =>
        new(new NavigationPage(page)
        {
            BarBackgroundColor = Color.FromArgb("#1A1525"),
            BarTextColor = Colors.White,
        })
        {
            Title = "Watchlist",
        };
}
