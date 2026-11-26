namespace Lab3;

/// <summary>Full activity feed, newest first. Shown as a bottom tab.</summary>
public partial class ActivityPage : ContentPage
{
    private readonly AppServices _services;

    public ActivityPage(AppServices services)
    {
        InitializeComponent();
        _services = services;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        Refresh();
    }

    private void OnClearClicked(object sender, EventArgs e)
    {
        _services.Activity.Clear();
        Refresh();
    }

    private void Refresh()
    {
        var entries = _services.Activity.Entries.Reverse().ToArray();
        FeedList.ItemsSource = entries;
        CountLabel.Text = entries.Length == 0
            ? "Nothing yet this session"
            : $"{entries.Length} recent change{(entries.Length == 1 ? "" : "s")}";
    }
}
