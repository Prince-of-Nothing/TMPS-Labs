using Watchlist.Core.CoverSearch;
using Watchlist.Core.CoverSearch.Images;
using Watchlist.Core.Domain;
using Watchlist.Core.Storage;

namespace Lab2;

public sealed class ConsoleApp(
    WatchlistRepository repository,
    CoverService covers,
    string vaultPath)
{
    public async Task RunAsync()
    {
        Console.WriteLine("Consumed Media Watchlist — Lab 2 (structural patterns)");
        Console.WriteLine($"Vault: {vaultPath}");
        Console.WriteLine("The library is plain Markdown; open index.md without this app.");
        PrintErrors();
        Console.WriteLine("Type 'help' for commands.");

        while (true)
        {
            Console.WriteLine();
            Console.Write("watchlist> ");
            var command = Console.ReadLine()?.Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(command))
            {
                continue;
            }

            try
            {
                switch (command)
                {
                    case "list":
                        List();
                        break;
                    case "show":
                        Show();
                        break;
                    case "add":
                        Add();
                        break;
                    case "edit":
                        Edit();
                        break;
                    case "consume":
                        Consume();
                        break;
                    case "rate":
                        Rate();
                        break;
                    case "reopen":
                        Reopen();
                        break;
                    case "delete":
                        Delete();
                        break;
                    case "search":
                        await SearchCoverAsync();
                        break;
                    case "cover":
                        ShowCover();
                        break;
                    case "reload":
                        repository.Reload();
                        PrintErrors();
                        Console.WriteLine($"Reloaded {repository.Items.Count} entries.");
                        break;
                    case "patterns":
                        Patterns();
                        break;
                    case "help":
                        Help();
                        break;
                    case "quit":
                    case "exit":
                        return;
                    default:
                        Console.WriteLine("Unknown command. Type 'help'.");
                        break;
                }
            }
            catch (Exception ex) when (ex is InvalidOperationException
                                           or ArgumentException
                                           or IOException
                                           or HttpRequestException
                                           or CoverSearchException
                                           or CoverImageException
                                           or TaskCanceledException)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }

    private void List()
    {
        if (repository.Items.Count == 0)
        {
            Console.WriteLine("The watchlist is empty. Use 'add'.");
            return;
        }

        Console.WriteLine($"{"ID",-4} {"Status",-16} {"Title",-34} {"Kind",-8} {"Category",-16} {"Cover",-5} Tags");
        Console.WriteLine(new string('-', 110));
        foreach (var item in repository.Items)
        {
            var status = item.Status switch
            {
                MediaStatus.Rated => $"rated {item.Rating}/10",
                MediaStatus.Consumed => "consumed",
                _ => "planned",
            };
            Console.WriteLine(
                $"{item.Id,-4} {status,-16} {Truncate(item.Title, 34),-34} {item.Kind,-8} " +
                $"{Truncate(item.Category, 16),-16} {(item.CoverPath is null ? "-" : "yes"),-5} " +
                string.Join(", ", item.Tags));
        }
    }

    private void Show()
    {
        var item = Find(PromptId());
        if (item is null)
        {
            return;
        }

        Console.WriteLine($"#{item.Id} {item.Title}");
        Console.WriteLine($"Kind:     {item.Kind}");
        Console.WriteLine($"Category: {item.Category}");
        Console.WriteLine($"Tags:     {(item.Tags.Count == 0 ? "-" : string.Join(", ", item.Tags))}");
        Console.WriteLine($"Status:   {item.Status}" + (item.Rating is int rating ? $" ({rating}/10)" : ""));
        Console.WriteLine($"Progress: {item.Progress ?? "-"}");
        Console.WriteLine($"Source:   {item.SourceUrl ?? "-"}");
        Console.WriteLine($"Cover:    {item.CoverPath ?? "-"}");
        Console.WriteLine($"Updated:  {item.UpdatedAt:yyyy-MM-dd HH:mm}");
        if (!string.IsNullOrWhiteSpace(item.Notes))
        {
            Console.WriteLine();
            Console.WriteLine(item.Notes);
        }
    }

    private void Add()
    {
        var title = Prompt("Title: ");
        if (string.IsNullOrWhiteSpace(title))
        {
            Console.WriteLine("Title is required.");
            return;
        }

        var kind = ParseKind(Prompt("Kind (film/series/book/game/anime/manga/other, default other): "), MediaKind.Other);
        var category = Prompt("Category (blank = Uncategorized): ");
        var tags = SplitTags(Prompt("Tags (comma-separated): "));
        var progress = Prompt("Progress (optional, e.g. 3/12): ");
        var notes = Prompt("Notes (optional): ");

        var item = new MediaItemBuilder()
            .WithTitle(title)
            .WithKind(kind)
            .WithCategory(category)
            .WithTags(tags)
            .WithProgress(progress)
            .WithNotes(notes)
            .Build(repository.NextId);

        repository.Add(item);
        Console.WriteLine($"Added #{item.Id} — {item.Title}.");
    }

    private void Edit()
    {
        var item = Find(PromptId());
        if (item is null)
        {
            return;
        }

        var title = Prompt($"Title [{item.Title}]: ");
        var kindInput = Prompt($"Kind [{item.Kind}]: ");
        var category = Prompt($"Category [{item.Category}]: ");
        var tagsInput = Prompt($"Tags [{string.Join(", ", item.Tags)}] (blank keeps, - clears): ");
        var progress = Prompt($"Progress [{item.Progress ?? "-"}] (blank keeps, - clears): ");
        var notes = Prompt($"Notes [{Truncate(item.Notes.Replace('\n', ' '), 30)}] (blank keeps, - clears): ");

        item.Edit(
            string.IsNullOrWhiteSpace(title) ? item.Title : title,
            string.IsNullOrWhiteSpace(kindInput) ? item.Kind : ParseKind(kindInput, item.Kind),
            string.IsNullOrWhiteSpace(category) ? item.Category : category,
            string.IsNullOrWhiteSpace(tagsInput) ? item.Tags : tagsInput == "-" ? [] : SplitTags(tagsInput),
            string.IsNullOrWhiteSpace(notes) ? item.Notes : notes == "-" ? string.Empty : notes,
            string.IsNullOrWhiteSpace(progress) ? item.Progress : progress == "-" ? null : progress);

        repository.Update(item);
        Console.WriteLine($"Updated #{item.Id}.");
    }

    private void Consume()
    {
        var item = Find(PromptId());
        if (item is null)
        {
            return;
        }

        item.MarkConsumed();
        repository.Update(item);
        Console.WriteLine($"#{item.Id} is consumed.");
    }

    private void Rate()
    {
        var item = Find(PromptId());
        if (item is null)
        {
            return;
        }

        var value = Prompt("Rating (1-10): ");
        if (!int.TryParse(value, out var rating))
        {
            Console.WriteLine("Rating must be a number from 1 to 10.");
            return;
        }

        item.Rate(rating);
        repository.Update(item);
        Console.WriteLine($"#{item.Id} rated {rating}/10.");
    }

    private void Reopen()
    {
        var item = Find(PromptId());
        if (item is null)
        {
            return;
        }

        item.Reopen();
        repository.Update(item);
        Console.WriteLine($"#{item.Id} moved back to planned.");
    }

    private void Delete()
    {
        var item = Find(PromptId());
        if (item is null)
        {
            return;
        }

        if (!Confirm($"Delete #{item.Id} {item.Title} and its cover?"))
        {
            return;
        }

        repository.Delete(item.Id);
        Console.WriteLine("Deleted.");
    }

    private async Task SearchCoverAsync()
    {
        var item = Find(PromptId());
        if (item is null)
        {
            return;
        }

        var query = Prompt($"Search covers for [{item.Title}]: ");
        if (string.IsNullOrWhiteSpace(query))
        {
            query = item.Kind switch
            {
                MediaKind.Film or MediaKind.Series or MediaKind.Anime => $"{item.Title} poster",
                MediaKind.Book or MediaKind.Manga => $"{item.Title} cover",
                _ => item.Title,
            };
        }

        Console.WriteLine("Searching through the decorated catalog...");
        var hits = await covers.SearchAsync(query, item.Kind);
        if (hits.Count == 0)
        {
            Console.WriteLine("No covers found. Try a different query.");
            return;
        }

        for (var i = 0; i < hits.Count; i++)
        {
            var hit = hits[i];
            Console.WriteLine($"  [{i + 1}] {hit.Title}");
            Console.WriteLine($"      {hit.Provider} · {hit.ImageUrl}");
        }

        var choice = Prompt("Pick a cover number (blank cancels): ");
        if (!int.TryParse(choice, out var index) || index < 1 || index > hits.Count)
        {
            Console.WriteLine("Cancelled.");
            return;
        }

        var candidate = hits[index - 1];
        Console.WriteLine("Downloading through the image proxy...");
        var download = await covers.SaveAsync(item.Id, candidate);
        item.SetCover(download.RelativePath, candidate.ImageUrl);
        repository.Update(item);

        var absolute = Path.Combine(
            vaultPath,
            download.RelativePath.Replace('/', Path.DirectorySeparatorChar));
        Console.WriteLine($"Saved {download.RelativePath} ({download.Bytes / 1024.0:F1} KB).");
        Console.WriteLine($"Local file: {absolute}");
    }

    private void ShowCover()
    {
        var item = Find(PromptId());
        if (item?.CoverPath is null)
        {
            Console.WriteLine("This entry has no local cover.");
            return;
        }

        var absolute = Path.Combine(
            vaultPath,
            item.CoverPath.Replace('/', Path.DirectorySeparatorChar));
        Console.WriteLine(File.Exists(absolute)
            ? $"Cover: {absolute}"
            : $"Cover link is {item.CoverPath}, but the file is missing.");
    }

    private MediaItem? Find(int id) =>
        repository.Find(id) ?? PrintMissing(id);

    private MediaItem? PrintMissing(int id)
    {
        Console.WriteLine($"Entry #{id} was not found.");
        return null;
    }

    private static int PromptId()
    {
        var raw = Prompt("Entry id: ");
        return int.TryParse(raw, out var id) ? id : 0;
    }

    private void PrintErrors()
    {
        if (repository.Errors.Count == 0)
        {
            return;
        }

        Console.WriteLine("Vault warnings:");
        foreach (var error in repository.Errors)
        {
            Console.WriteLine($"  {error.FilePath}: {error.Message}");
        }
    }

    private static string Prompt(string label)
    {
        Console.Write(label);
        return Console.ReadLine()?.Trim() ?? string.Empty;
    }

    private static bool Confirm(string question)
    {
        Console.Write($"{question} [y/N] ");
        return Console.ReadLine()?.Trim().Equals("y", StringComparison.OrdinalIgnoreCase) == true;
    }

    private static MediaKind ParseKind(string value, MediaKind fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        return value.Trim().ToLowerInvariant() switch
        {
            "movie" => MediaKind.Film,
            "tv" or "show" => MediaKind.Series,
            _ => Enum.TryParse<MediaKind>(value, ignoreCase: true, out var kind) ? kind : fallback,
        };
    }

    private static IReadOnlyList<string> SplitTags(string value) =>
        value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static string Truncate(string value, int length) =>
        value.Length <= length ? value : value[..(length - 1)] + "…";

    private static void Help()
    {
        Console.WriteLine("""
            list                 show every entry
            show                 show one entry and its notes
            add                  add an entry
            edit                 change title/kind/category/tags/notes
            consume              mark an entry consumed
            rate                 rate a consumed entry 1-10
            reopen               move an entry back to planned
            delete               delete an entry and its cover
            search               search a cover and download the chosen one
            cover                print the local cover path
            reload               re-read the Markdown vault
            patterns             explain the structural pattern chain
            help                 show this help
            quit                 exit
            """);
    }

    private static void Patterns()
    {
        Console.WriteLine("""
            Cover search path:
              ConsoleApp
                -> ICoverCatalog (FilteringCoverCatalog)
                -> ICoverCatalog (CachingCoverCatalog)
                -> RoutingCoverCatalog
                -> WikimediaCoverAdapter / OpenLibraryCoverAdapter / TvMazeCoverAdapter
                -> public provider API

            Cover download path:
              ConsoleApp -> CoverService
                -> ICoverImage (CachedCoverImageProxy)
                -> ICoverImage (RemoteCoverImage)
                -> covers/<id>.jpg

            Adapter translates provider DTOs, Decorators add filtering and caching,
            and the Proxy keeps the same image interface while serving from disk.
            """);
    }
}
