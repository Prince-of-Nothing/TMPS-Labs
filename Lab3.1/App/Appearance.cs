using Microsoft.Maui.Storage;

namespace Lab3;

/// <summary>User-facing preferences. Every value has a safe default.</summary>
public static class AppPreferences
{
    public const string ThemeKey = "theme";
    public const string AccentKey = "accent";
    public const string AmoledKey = "amoled";
    public const string DefaultViewKey = "default_view";
    public const string DefaultSortKey = "default_sort";
    public const string ConfirmDeleteKey = "confirm_delete";

    public static readonly string[] SortOptions = ["Title", "Rating", "Recently updated", "Kind"];
    public const string GridView = "Grid";
    public const string ListView = "List";

    /// <summary>Raised after any library-facing preference changes so open pages can refresh.</summary>
    public static event Action? Changed;

    public static string Theme => Preferences.Default.Get(ThemeKey, "System");
    public static string Accent => Preferences.Default.Get(AccentKey, AccentChoice.Default.Key);
    public static bool Amoled => Preferences.Default.Get(AmoledKey, false);
    public static string DefaultView => Preferences.Default.Get(DefaultViewKey, ListView);
    public static string DefaultSort => Preferences.Default.Get(DefaultSortKey, SortOptions[0]);
    public static bool ConfirmDelete => Preferences.Default.Get(ConfirmDeleteKey, true);

    public static void SetTheme(string theme) => Save(ThemeKey, theme);
    public static void SetAccent(string key) => Save(AccentKey, key);
    public static void SetAmoled(bool enabled) => Save(AmoledKey, enabled);
    public static void SetDefaultView(string view) => Save(DefaultViewKey, view);
    public static void SetDefaultSort(string sort) => Save(DefaultSortKey, sort);
    public static void SetConfirmDelete(bool enabled) => Save(ConfirmDeleteKey, enabled);

    private static void Save(string key, string value)
    {
        Preferences.Default.Set(key, value);
        Changed?.Invoke();
    }

    private static void Save(string key, bool value)
    {
        Preferences.Default.Set(key, value);
        Changed?.Invoke();
    }
}

/// <summary>
/// Rewrites the palette keys used by every page. Light, dark and AMOLED black
/// are the three variants; AMOLED only applies when the effective theme is dark.
/// </summary>
public static class ThemePalette
{
    // key, light, dark, amoled-black (amoled is used only in dark mode)
    private static readonly (string Key, string Light, string Dark, string Amoled)[] Entries =
    [
        ("PageBg", "#F8F6F3", "#121016", "#000000"),
        ("CardBg", "#FFFFFF", "#1E1B24", "#111016"),
        ("CoverBg", "#F1EDE7", "#2A2632", "#1B1922"),
        ("StrokeBg", "#EAE6DF", "#393343", "#2A2632"),
        ("TextPrimary", "#1A1525", "#F4EEF7", "#F4EEF7"),
        ("TextMuted", "#8A7F8C", "#B9AFBF", "#B9AFBF"),
        ("TextFaint", "#A0899C", "#C9BDCF", "#C9BDCF"),
        ("LabelPlum", "#6B5B7A", "#CDB9DB", "#CDB9DB"),
        ("LabelRose", "#8C4A63", "#E3A6C1", "#E3A6C1"),
        ("LabelGreen", "#3F6B46", "#9BD0A6", "#9BD0A6"),
    ];

    /// <summary>Applies the palette for the effective theme. Call again whenever the theme changes.</summary>
    public static void Apply(ResourceDictionary resources, bool isDark, bool amoled)
    {
        foreach (var (key, light, dark, black) in Entries)
        {
            var hex = !isDark ? light : amoled ? black : dark;
            resources[key] = Color.FromArgb(hex);
        }
    }

    public static bool IsDark(Application app) => app.RequestedTheme == AppTheme.Dark;
}
