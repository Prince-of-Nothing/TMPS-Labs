namespace Lab0;

public sealed class WatchlistConsoleApp(WatchlistService watchlist)
{
    public void Run()
    {
        Console.WriteLine("Consumed Media Watchlist");
        PrintHelp();

        while (true)
        {
            Console.Write("\n> ");
            var command = Console.ReadLine()?.Trim().ToLowerInvariant();
            if (command is null or "quit" or "exit")
            {
                return;
            }

            try
            {
                switch (command)
                {
                    case "help": PrintHelp(); break;
                    case "category": AddCategory(); break;
                    case "tag": AddTag(); break;
                    case "add": AddMedia(); break;
                    case "edit": EditMedia(); break;
                    case "consumed": MarkAsConsumed(); break;
                    case "rate": Rate(); break;
                    case "list": PrintItems(); break;
                    default: Console.WriteLine("Unknown command. Type 'help' for options."); break;
                }
            }
            catch (ArgumentException exception)
            {
                Console.WriteLine($"Invalid input: {exception.Message}");
            }
            catch (InvalidOperationException exception)
            {
                Console.WriteLine(exception.Message);
            }
            catch (KeyNotFoundException exception)
            {
                Console.WriteLine(exception.Message);
            }
        }
    }

    private void AddCategory()
    {
        var category = watchlist.AddCategory(Read("Category name"));
        Console.WriteLine($"Category available as #{category.Id}: {category.Name}");
    }

    private void AddTag()
    {
        var tag = watchlist.AddTag(Read("Tag name"));
        Console.WriteLine($"Tag available as #{tag.Id}: {tag.Name}");
    }

    private void AddMedia()
    {
        Console.WriteLine($"Categories: {FormatLabels(watchlist.Categories)}");
        Console.WriteLine($"Tags: {FormatLabels(watchlist.Tags)}");
        watchlist.AddMedia(Read("Title"), Read("Category"), Read("Tags (comma-separated; optional)"));
        Console.WriteLine("Media added to your watchlist.");
    }

    private void MarkAsConsumed()
    {
        watchlist.MarkAsConsumed(ReadInteger("Media ID"));
        Console.WriteLine("Marked as consumed.");
    }

    private void EditMedia()
    {
        var id = ReadInteger("Media ID");
        var item = watchlist.Items.FirstOrDefault(candidate => candidate.Id == id)
            ?? throw new KeyNotFoundException($"No media item has ID {id}.");

        Console.WriteLine($"Current: {item.Title} | {FormatLabel(item.Category)} | {FormatLabels(item.Tags)}");
        Console.WriteLine($"Categories: {FormatLabels(watchlist.Categories)}");
        Console.WriteLine($"Tags: {FormatLabels(watchlist.Tags)}");
        Console.WriteLine("Use a name or #ID. New names are created automatically, ignoring case.");
        Console.WriteLine("Leave a field blank to keep it. Enter '-' for tags to remove all tags.");

        var title = ReadOrKeep("Title", item.Title);
        var category = ReadOrKeep("Category", item.Category.Name);
        var enteredTags = Read("Tags (comma-separated)");
        var tags = enteredTags == "-"
            ? string.Empty
            : string.IsNullOrWhiteSpace(enteredTags)
                ? string.Join(", ", item.Tags)
                : enteredTags;

        watchlist.UpdateMedia(id, title, category, tags);
        Console.WriteLine("Media details updated.");
    }

    private void Rate()
    {
        watchlist.Rate(ReadInteger("Media ID"), ReadInteger("Rating from 1 to 10"));
        Console.WriteLine("Rating saved.");
    }

    private void PrintItems()
    {
        if (watchlist.Items.Count == 0)
        {
            Console.WriteLine("Your watchlist is empty.");
            return;
        }

        foreach (var item in watchlist.Items)
        {
            var tags = item.Tags.Count == 0
                ? "no tags"
                : string.Join(", ", item.Tags.Select(FormatLabel));
            var rating = item.Rating is int value
                ? $"{value}/10 (score {watchlist.GetScore(item)}/100)"
                : "not rated";
            var status = item.IsConsumed ? "consumed" : "planned";

            Console.WriteLine($"#{item.Id} | {item.Title} | {FormatLabel(item.Category)} | {status} | {rating} | {tags}");
        }
    }

    private static string Read(string label)
    {
        Console.Write($"{label}: ");
        return Console.ReadLine() ?? string.Empty;
    }

    private static string ReadOrKeep(string label, string currentValue)
    {
        Console.Write($"{label} [{currentValue}]: ");
        var input = Console.ReadLine();
        return string.IsNullOrWhiteSpace(input) ? currentValue : input.Trim();
    }

    private static int ReadInteger(string label)
    {
        var input = Read(label);
        return int.TryParse(input, out var value)
            ? value
            : throw new ArgumentException("Enter a whole number.");
    }

    private static string FormatLabels(IEnumerable<MediaLabel> labels)
    {
        var formatted = string.Join(", ", labels.OrderBy(label => label.Id).Select(FormatLabel));
        return formatted.Length == 0 ? "(none yet)" : formatted;
    }

    private static string FormatLabel(MediaLabel label) => $"#{label.Id} {label.Name}";

    private static void PrintHelp()
    {
        Console.WriteLine(
            """
            Commands:
              category  Create or reuse a category
              tag       Create or reuse a tag
              add       Add a title to your watchlist
              edit      Modify a title, category, or tags
              consumed  Mark an item as consumed
              rate      Rate consumed media from 1 to 10
              list      Show your media and scores
              help      Show commands
              quit      Exit

            Use category/tag names or IDs when adding or editing media.
            New names are created automatically; matching ignores letter case.
            """);
    }
}
