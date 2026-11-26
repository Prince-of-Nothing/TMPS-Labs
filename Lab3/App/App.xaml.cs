using Microsoft.Maui.Storage;

namespace Lab3;

public partial class App : Application
{
    public const string AccentPreferenceKey = "accent";

    private AppServices? _services;

    public App()
    {
        InitializeComponent();
        UserAppTheme = ThemeFromPreference(Preferences.Default.Get("theme", "System"));
        Resources["Plum"] = AccentChoice.Find(
            Preferences.Default.Get(AccentPreferenceKey, AccentChoice.Default.Key)).Color;
    }

    public static AppTheme ThemeFromPreference(string theme) => theme switch
    {
        "Light" => AppTheme.Light,
        "Dark" => AppTheme.Dark,
        _ => AppTheme.Unspecified,
    };

    /// <summary>Replaces the Plum key that every accent-colored control reads.</summary>
    public static void ApplyAccent(string key)
    {
        var choice = AccentChoice.Find(key);
        if (Current is { } app)
        {
            app.Resources["Plum"] = choice.Color;
        }
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

    private void OnStorageChosen(string root) => SwitchLibrary(root, null);

    /// <summary>Opens the library at <paramref name="root"/>, remembers it, and optionally shows a notice.</summary>
    public void SwitchLibrary(string root, string? notice)
    {
        Preferences.Default.Set(StorageLocations.PreferenceKey, root);
        _services = AppServices.Create(root);
        var window = Windows.FirstOrDefault();
        if (window is not null)
        {
            var main = new MainPage(_services);
            if (notice is not null)
            {
                main.ShowNotice(notice);
            }

            window.Page = NavigationRoot(main);
        }
    }

    private static Window MainWindow(Page page) =>
        new(NavigationRoot(page))
        {
            Title = "Watchlist",
        };

    private static NavigationPage NavigationRoot(Page page) =>
        new(page)
        {
            BarBackgroundColor = Color.FromArgb("#1A1525"),
            BarTextColor = Colors.White,
        };
}

/// <summary>Accent colors offered in Settings, in the order they are shown.</summary>
public sealed record AccentChoice(string Key, string Name, Color Color)
{
    public static readonly AccentChoice[] All =
    [
        new("plum", "Plum", Color.FromArgb("#6B5B7A")),
        new("rose", "Rose", Color.FromArgb("#8C4A63")),
        new("teal", "Teal", Color.FromArgb("#3E7C7A")),
        new("forest", "Forest", Color.FromArgb("#3F6B46")),
        new("amber", "Amber", Color.FromArgb("#9A6B2F")),
    ];

    public static AccentChoice Default => All[0];

    public static AccentChoice Find(string key) =>
        All.FirstOrDefault(choice => choice.Key == key) ?? Default;
}
