using Watchlist.Core.Storage;

namespace Lab3;

public partial class StorageSetupPage : ContentPage
{
    public StorageSetupPage()
    {
        InitializeComponent();
        var shared = StorageLocations.SharedDocuments;
        SharedPathLabel.Text = shared.Length == 0 ? "Shared Documents is unavailable on this device." : shared;
        AppPathLabel.Text = StorageLocations.AppStorage;
        UpdatePermissionLabel();
    }

    public event Action<string>? Chosen;

    protected override void OnAppearing()
    {
        base.OnAppearing();
        UpdatePermissionLabel();
    }

    private async void OnSharedClicked(object sender, EventArgs e)
    {
        StatusLabel.Text = "Checking access...";
        if (!await StorageLocations.RequestSharedAccessAsync())
        {
            UpdatePermissionLabel();
            StatusLabel.Text = OperatingSystem.IsAndroidVersionAtLeast(30)
                ? "Enable “All files access” for Watchlist in Settings, then tap the button again."
                : "Storage permission was not granted. You can still use app storage.";
            return;
        }

        var root = StorageLocations.SharedDocuments;
        if (!StorageLocations.CanUse(root))
        {
            StatusLabel.Text = "That folder is not writable. Check the permission and try again.";
            return;
        }

        var copied = VaultMigrator.CopyVault(StorageLocations.AppStorage, root);
        StatusLabel.Text = copied.Total == 0
            ? "Using shared Documents."
            : $"Copied {copied.Entries} entries and {copied.Covers} covers to shared Documents.";
        Chosen?.Invoke(root);
    }

    private void OnAppClicked(object sender, EventArgs e)
    {
        var root = StorageLocations.AppStorage;
        if (!StorageLocations.CanUse(root))
        {
            StatusLabel.Text = "App storage is not writable. Reinstall the app and try again.";
            return;
        }

        StatusLabel.Text = "Using app storage.";
        Chosen?.Invoke(root);
    }

    private void UpdatePermissionLabel()
    {
        PermissionLabel.Text = StorageLocations.HasSharedAccess
            ? "Access: granted"
            : "Access: not granted yet";
    }
}
