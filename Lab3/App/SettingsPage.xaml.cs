using Microsoft.Maui.Storage;
using Watchlist.Core.CoverSearch;
using Watchlist.Core.Storage;

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
        ThemePicker.SelectedItem = AppPreferences.Theme;
        AmoledSwitch.IsToggled = AppPreferences.Amoled;

        DefaultViewPicker.ItemsSource = new List<string> { AppPreferences.ListView, AppPreferences.GridView };
        DefaultViewPicker.SelectedItem = AppPreferences.DefaultView;
        DefaultSortPicker.ItemsSource = AppPreferences.SortOptions.ToList();
        DefaultSortPicker.SelectedItem = AppPreferences.DefaultSort;
        ConfirmDeleteSwitch.IsToggled = AppPreferences.ConfirmDelete;

        BuildAccentButtons();

        ProviderPicker.ItemsSource = ProviderNames;
        ProviderPicker.SelectedIndex = (int)_services.CoverProvider;

        StorageLabel.Text = $"Current: {_services.RootPath}";
        SharedButton.Text = StorageLocations.SharedDocuments.Length == 0
            ? "Shared Documents is unavailable"
            : "Move to shared Documents";
        _loaded = true;
    }

    private void OnThemeChanged(object sender, EventArgs e)
    {
        if (!_loaded || ThemePicker.SelectedItem is not string theme)
        {
            return;
        }

        AppPreferences.SetTheme(theme);
        StatusLabel.Text = $"Theme mode set to {theme}.";
    }

    private void OnAmoledToggled(object sender, ToggledEventArgs e)
    {
        if (!_loaded)
        {
            return;
        }

        AppPreferences.SetAmoled(e.Value);
        StatusLabel.Text = e.Value ? "AMOLED black is on." : "AMOLED black is off.";
    }

    private void OnDefaultViewChanged(object sender, EventArgs e)
    {
        if (!_loaded || DefaultViewPicker.SelectedItem is not string view)
        {
            return;
        }

        AppPreferences.SetDefaultView(view);
        StatusLabel.Text = $"Library opens in {view.ToLowerInvariant()} view.";
    }

    private void OnDefaultSortChanged(object sender, EventArgs e)
    {
        if (!_loaded || DefaultSortPicker.SelectedItem is not string sort)
        {
            return;
        }

        AppPreferences.SetDefaultSort(sort);
        StatusLabel.Text = $"Library sorts by {sort.ToLowerInvariant()} by default.";
    }

    private void OnConfirmDeleteToggled(object sender, ToggledEventArgs e)
    {
        if (!_loaded)
        {
            return;
        }

        AppPreferences.SetConfirmDelete(e.Value);
        StatusLabel.Text = e.Value ? "You will be asked before deleting." : "Deletes happen without asking.";
    }

    private void BuildAccentButtons()
    {
        var current = AppPreferences.Accent;
        AccentRow.Children.Clear();
        foreach (var choice in AccentChoice.All)
        {
            var selected = choice.Key == current;
            var button = new Button
            {
                Text = selected ? $"✓ {choice.Name}" : choice.Name,
                BackgroundColor = choice.Color,
                TextColor = Colors.White,
                FontAttributes = selected ? FontAttributes.Bold : FontAttributes.None,
                Margin = new Thickness(0, 0, 8, 8),
            };
            SemanticProperties.SetDescription(button, $"{choice.Name} accent color");
            button.Clicked += (_, _) => OnAccentClicked(choice);
            AccentRow.Children.Add(button);
        }
    }

    private void OnAccentClicked(AccentChoice choice)
    {
        AppPreferences.SetAccent(choice.Key);
        BuildAccentButtons();
        StatusLabel.Text = $"Accent set to {choice.Name}.";
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

    private async void OnAppStorageClicked(object sender, EventArgs e) =>
        await MoveLibraryAsync(StorageLocations.AppStorage, "app storage");

    private async void OnSharedClicked(object sender, EventArgs e)
    {
        var root = StorageLocations.SharedDocuments;
        if (root.Length == 0)
        {
            StatusLabel.Text = "Shared Documents is unavailable on this device.";
            return;
        }

        if (!await StorageLocations.RequestSharedAccessAsync())
        {
            StatusLabel.Text = "Enable “All files access” for Watchlist in Settings, then try again.";
            return;
        }

        await MoveLibraryAsync(root, "shared Documents");
    }

    private async void OnCustomClicked(object sender, EventArgs e)
    {
        var path = CustomPathEntry.Text?.Trim() ?? string.Empty;
        if (!Path.IsPathFullyQualified(path))
        {
            StatusLabel.Text = "Enter a full folder path, such as /storage/emulated/0/Notes/Watchlist.";
            return;
        }

        if (path.StartsWith("/storage/", StringComparison.Ordinal) &&
            !await StorageLocations.RequestSharedAccessAsync())
        {
            StatusLabel.Text = "Enable “All files access” for Watchlist in Settings, then try again.";
            return;
        }

        await MoveLibraryAsync(path, "that folder");
    }

    /// <summary>
    /// Copies the vault into <paramref name="target"/> without overwriting anything,
    /// then reopens the app on the new folder. The old copy is never deleted.
    /// </summary>
    private async Task MoveLibraryAsync(string target, string label)
    {
        var source = _services.RootPath;
        if (string.Equals(Path.GetFullPath(source), Path.GetFullPath(target), StringComparison.OrdinalIgnoreCase))
        {
            StatusLabel.Text = "The library is already in that folder.";
            return;
        }

        if (!StorageLocations.CanUse(target))
        {
            StatusLabel.Text = "That folder is not writable. Check the path and the permission.";
            return;
        }

        var confirmed = await DisplayAlertAsync(
            "Move library",
            $"Copy your library to {label}?\n\n{target}\n\nThe current copy stays where it is.",
            "Move",
            "Cancel");
        if (!confirmed)
        {
            return;
        }

        try
        {
            var copied = VaultMigrator.CopyVault(source, target);
            var notice = copied.Total == 0
                ? $"Library now opens from {label}."
                : $"Copied {copied.Entries} entries and {copied.Covers} covers to {label}.";

            if (Application.Current is App app)
            {
                app.SwitchLibrary(target, notice);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            StatusLabel.Text = $"Move failed: {ex.Message}";
        }
    }
}
