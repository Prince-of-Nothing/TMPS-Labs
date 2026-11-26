using Watchlist.Core.Domain;

namespace Lab2;

public partial class EditPage : ContentPage
{
    public EditPage(MediaItem? item)
    {
        InitializeComponent();
        KindPicker.ItemsSource = Enum.GetValues<MediaKind>().Select(value => value.ToString()).ToList();

        if (item is null)
        {
            HeaderLabel.Text = "New entry";
            KindPicker.SelectedItem = MediaKind.Other.ToString();
            return;
        }

        HeaderLabel.Text = "Edit entry";
        TitleEntry.Text = item.Title;
        KindPicker.SelectedItem = item.Kind.ToString();
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
            string.IsNullOrWhiteSpace(ProgressEntry.Text) ? null : ProgressEntry.Text.Trim()));
        await Navigation.PopAsync();
    }

    private async void OnCancelClicked(object sender, EventArgs e) => await Navigation.PopAsync();
}
