using Watchlist.Core.Domain;

namespace Lab2;

public partial class EditPage : ContentPage
{
    // Lab 2 has no custom-kind input; keep whatever the shared vault already stored.
    private readonly string? _customKind;

    public EditPage(MediaItem? item)
    {
        InitializeComponent();
        _customKind = item?.CustomKind;
        KindPicker.ItemsSource = Enum.GetValues<MediaKind>()
            .Where(value => value != MediaKind.Custom)
            .Select(value => value.ToString())
            .ToList();

        if (item is null)
        {
            HeaderLabel.Text = "New entry";
            KindPicker.SelectedItem = MediaKind.Other.ToString();
            return;
        }

        HeaderLabel.Text = "Edit entry";
        TitleEntry.Text = item.Title;
        KindPicker.SelectedItem = item.Kind == MediaKind.Custom
            ? MediaKind.Other.ToString()
            : item.Kind.ToString();
        CategoryEntry.Text = item.Category;
        TagsEntry.Text = string.Join(", ", item.Tags);
        ProgressEntry.Text = item.Progress ?? "";
        NotesEditor.Text = item.Notes;
    }

    public event Action<EntryDraft>? Saved;

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        var title = TitleEntry.Text?.Trim() ?? "";
        if (title.Length == 0)
        {
            StatusLabel.Text = "A title is required.";
            return;
        }

        var kind = Enum.TryParse<MediaKind>(KindPicker.SelectedItem as string, out var parsed)
            ? parsed
            : MediaKind.Other;
        Saved?.Invoke(new EntryDraft(
            title,
            kind,
            CategoryEntry.Text?.Trim() ?? "",
            TagsEntry.Text?.Trim() ?? "",
            NotesEditor.Text?.Trim() ?? "",
            string.IsNullOrWhiteSpace(ProgressEntry.Text) ? null : ProgressEntry.Text.Trim(),
            _customKind));
        await Navigation.PopAsync();
    }

    private async void OnCancelClicked(object sender, EventArgs e) => await Navigation.PopAsync();
}
