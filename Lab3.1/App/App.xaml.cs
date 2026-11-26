using Microsoft.Maui.Storage;

namespace Lab3;

public partial class App : Application
{
    private AppServices? _services;

    public App()
    {
        InitializeComponent();
        ApplyAppearance();
        AppPreferences.Changed += ApplyAppearance;
        RequestedThemeChanged += (_, _) => ApplyAppearance();
    }

    public static AppTheme ThemeFromPreference(string theme) => theme switch
    {
        "Light" => AppTheme.Light,
        "Dark" => AppTheme.Dark,
        _ => AppTheme.Unspecified,
    };

    /// <summary>
    /// Re-applies theme mode, AMOLED black and accent from preferences. Safe to
    /// call often: pages bound to palette keys update without being rebuilt.
    /// </summary>
    public void ApplyAppearance()
    {
        UserAppTheme = ThemeFromPreference(AppPreferences.Theme);
        Resources["Plum"] = AccentChoice.Find(AppPreferences.Accent).Color;
        ThemePalette.Apply(Resources, ThemePalette.IsDark(this), AppPreferences.Amoled);
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var overridePath = Environment.GetEnvironmentVariable("TMPS_LAB3_LIBRARY");
        if (!string.IsNullOrWhiteSpace(overridePath) && StorageLocations.CanUse(overridePath))
        {
            _services ??= AppServices.Create(overridePath);
            return MainWindow(RootTabs(_services, null));
        }

        var saved = Preferences.Default.Get(StorageLocations.PreferenceKey, string.Empty);
        if (!string.IsNullOrWhiteSpace(saved) && StorageLocations.CanUse(saved))
        {
            _services ??= AppServices.Create(saved);
            return MainWindow(RootTabs(_services, null));
        }

        var setup = new StorageSetupPage();
        setup.Chosen += OnStorageChosen;
        return MainWindow(new NavigationPage(setup)
        {
            BarBackgroundColor = Color.FromArgb("#1A1525"),
            BarTextColor = Colors.White,
        });
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
            window.Page = RootTabs(_services, notice);
        }
    }

    /// <summary>
    /// Bottom tabs in the Aniyomi style: Library, Activity and Settings. Each tab
    /// has its own navigation stack, so Import, Edit and Cover pages push inside it.
    /// </summary>
    private static Page RootTabs(AppServices services, string? notice)
    {
        var library = new MainPage(services);
        if (notice is not null)
        {
            library.ShowNotice(notice);
        }

        var tabs = new TabbedPage();
        tabs.SetDynamicResource(TabbedPage.BarBackgroundColorProperty, "CardBg");
        tabs.SetDynamicResource(TabbedPage.SelectedTabColorProperty, "Plum");
        tabs.SetDynamicResource(TabbedPage.UnselectedTabColorProperty, "TextMuted");

        tabs.Children.Add(Tab(library, "Library"));
        tabs.Children.Add(Tab(new ActivityPage(services), "Activity"));
        tabs.Children.Add(Tab(new SettingsPage(services), "Settings"));
        return tabs;
    }

    private static NavigationPage Tab(Page page, string title) =>
        new(page)
        {
            Title = title,
            BarBackgroundColor = Color.FromArgb("#1A1525"),
            BarTextColor = Colors.White,
        };

    private static Window MainWindow(Page page) =>
        new(page)
        {
            Title = "Watchlist",
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
