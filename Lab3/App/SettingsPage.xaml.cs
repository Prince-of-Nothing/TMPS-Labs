using Microsoft.Maui.Storage;
using Watchlist.Core.CoverSearch;

namespace Lab3;

public partial class SettingsPage : ContentPage
{
    // Same order as CoverProvider: Auto, Wikimedia, OpenLibrary, TvMaze.
    private static readonly List<string> ProviderNames =
        ["Auto", "Wikimedia", "Open Library", "TVmaze"];

    private readonly AppServices _services;
    private bool _loaded;

    public SettingsPage(AppServices services)
    {
        InitializeComponent();
        _services = services;

        ThemePicker.ItemsSource = new List<string> { "System", "Light", "Dark" };
        ThemePicker.SelectedItem = Preferences.Default.Get("theme", "System");

        ProviderPicker.ItemsSource = ProviderNames;
        ProviderPicker.SelectedIndex = (int)_services.CoverProvider;
        StorageLabel.Text = _services.RootPath;
        _loaded = true;
    }

    private void OnThemeChanged(object sender, EventArgs e)
    {
        if (!_loaded || ThemePicker.SelectedItem is not string theme)
        {
            return;
        }

        Preferences.Default.Set("theme", theme);
        Application.Current!.UserAppTheme = theme switch
        {
            "Light" => AppTheme.Light,
            "Dark" => AppTheme.Dark,
            _ => AppTheme.Unspecified,
        };
        StatusLabel.Text = $"Theme set to {theme}.";
    }

    private void OnProviderChanged(object sender, EventArgs e)
    {
        if (!_loaded || ProviderPicker.SelectedIndex < 0)
        {
            return;
        }

        var provider = (CoverProvider)ProviderPicker.SelectedIndex;
        _services.SetCoverProvider(provider);
        StatusLabel.Text = provider == CoverProvider.Auto
            ? "Cover search routes automatically by kind."
            : $"{ProviderNames[ProviderPicker.SelectedIndex]} is now the cover search engine.";
    }
}
