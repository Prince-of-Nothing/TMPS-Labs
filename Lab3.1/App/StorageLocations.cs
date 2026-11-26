using Android.Content;
using Android.Provider;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage;

namespace Lab3;

/// <summary>
/// The two supported library locations and the Android permission needed for
/// the shared one. App storage always works; shared Documents is opt-in.
/// </summary>
public static class StorageLocations
{
    public const string PreferenceKey = "library_root";

    public static string AppStorage => Path.Combine(FileSystem.AppDataDirectory, "library");

    public static string SharedDocuments
    {
        get
        {
#pragma warning disable CA1422 // Required for a user-visible folder on all supported APIs.
            var documents = Android.OS.Environment.GetExternalStoragePublicDirectory(
                Android.OS.Environment.DirectoryDocuments);
#pragma warning restore CA1422
            return string.IsNullOrWhiteSpace(documents?.AbsolutePath)
                ? string.Empty
                : Path.Combine(documents.AbsolutePath, "TMPS-Watchlist");
        }
    }

    public static bool HasSharedAccess =>
        OperatingSystem.IsAndroidVersionAtLeast(30)
            ? Android.OS.Environment.IsExternalStorageManager
            : Platform.AppContext.CheckSelfPermission(Android.Manifest.Permission.WriteExternalStorage)
              == Android.Content.PM.Permission.Granted;

    /// <summary>
    /// On Android 11+ the user grants "All files access" in Settings; on older
    /// versions the normal runtime storage permission is enough.
    /// </summary>
    public static async Task<bool> RequestSharedAccessAsync()
    {
        if (HasSharedAccess)
        {
            return true;
        }

        if (OperatingSystem.IsAndroidVersionAtLeast(30))
        {
            var activity = Platform.CurrentActivity;
            if (activity is null)
            {
                return false;
            }

            var intent = new Intent(Settings.ActionManageAppAllFilesAccessPermission);
            intent.SetData(Android.Net.Uri.Parse($"package:{AppInfo.Current.PackageName}"));
            activity.StartActivity(intent);
            return false;
        }

        var status = await Permissions.RequestAsync<Permissions.StorageWrite>();
        return status == PermissionStatus.Granted;
    }

    public static bool CanUse(string rootPath)
    {
        if (string.IsNullOrWhiteSpace(rootPath))
        {
            return false;
        }

        try
        {
            Directory.CreateDirectory(rootPath);
            var probe = Path.Combine(rootPath, $".write-test-{Guid.NewGuid():N}");
            File.WriteAllText(probe, "ok");
            File.Delete(probe);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            return false;
        }
    }
}
