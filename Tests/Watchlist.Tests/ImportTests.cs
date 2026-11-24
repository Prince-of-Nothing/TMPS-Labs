using Watchlist.Behavioral.Import;
using Watchlist.Core.Domain;

namespace Watchlist.Tests;

public sealed class ImportTests
{
    private const string AniListJson = """
    {
      "data": {
        "anime": {
          "lists": [{
            "name": "Completed TV",
            "entries": [
              {
                "status": "COMPLETED",
                "score": 8,
                "progress": 26,
                "media": {
                  "id": 1,
                  "siteUrl": "https://anilist.co/anime/1",
                  "format": "TV",
                  "episodes": 26,
                  "genres": ["Action", "Drama"],
                  "title": { "romaji": "Cowboy Bebop", "english": "Cowboy Bebop" },
                  "coverImage": { "extraLarge": "https://img.example/bebop.jpg" }
                }
              },
              {
                "status": "CURRENT",
                "score": 0,
                "progress": 3,
                "media": {
                  "id": 2,
                  "siteUrl": "https://anilist.co/anime/2",
                  "format": "MOVIE",
                  "episodes": 1,
                  "genres": [],
                  "title": { "romaji": "A Movie" },
                  "coverImage": { "large": "https://img.example/movie.jpg" }
                }
              }
            ]
          }]
        },
        "manga": {
          "lists": [{
            "name": "Reading",
            "entries": [{
              "status": "CURRENT",
              "score": 0,
              "progress": 10,
              "progressVolumes": 2,
              "media": {
                "id": 3,
                "siteUrl": "https://anilist.co/manga/3",
                "format": "MANGA",
                "chapters": 100,
                "volumes": 12,
                "genres": ["Fantasy"],
                "title": { "romaji": "Berserk" },
                "coverImage": { "large": "https://img.example/berserk.jpg" }
              }
            }]
          }]
        }
      }
    }
    """;

    private const string MalXml = """
    <?xml version="1.0" encoding="UTF-8"?>
    <myanimelist>
      <myinfo><user_name>dinu</user_name></myinfo>
      <anime>
        <series_animedb_id>1</series_animedb_id>
        <series_title>Cowboy Bebop</series_title>
        <series_type>TV</series_type>
        <series_episodes>26</series_episodes>
        <my_watched_episodes>26</my_watched_episodes>
        <my_score>9</my_score>
        <my_status>Completed</my_status>
        <series_image>https://cdn.mal.example/bebop.jpg</series_image>
      </anime>
      <manga>
        <series_mangadb_id>2</series_mangadb_id>
        <series_title>Berserk</series_title>
        <series_type>Manga</series_type>
        <series_chapters>100</series_chapters>
        <my_read_chapters>10</my_read_chapters>
        <my_score>0</my_score>
        <my_status>Reading</my_status>
        <series_image>https://cdn.mal.example/berserk.jpg</series_image>
      </manga>
    </myanimelist>
    """;

    [Fact]
    public async Task AniList_importer_maps_status_score_format_and_cover()
    {
        var importer = new AniListImporter(StubHttpHandler.Json(AniListJson));

        var rows = await importer.ImportAsync("dinu");

        Assert.Equal(3, rows.Count);
        var bebop = rows[0];
        Assert.Equal("Cowboy Bebop", bebop.Title);
        Assert.Equal(MediaKind.Anime, bebop.Kind);
        Assert.Equal("TV", bebop.Category);
        Assert.Equal(MediaStatus.Rated, bebop.Status);
        Assert.Equal(8, bebop.Rating);
        Assert.Equal("26/26", bebop.Progress);
        Assert.Equal("https://img.example/bebop.jpg", bebop.CoverSourceUrl);
        Assert.Equal("COMPLETED", bebop.SourceStatus);

        var movie = rows[1];
        Assert.Equal(MediaStatus.Planned, movie.Status);
        Assert.Null(movie.Rating);
        Assert.Equal("Movie", movie.Category);

        var manga = rows[2];
        Assert.Equal(MediaKind.Manga, manga.Kind);
        Assert.Equal("10/100", manga.Progress);
    }

    [Fact]
    public async Task AniList_error_payload_is_reported()
    {
        var importer = new AniListImporter(StubHttpHandler.Json(
            """{ "errors": [{ "message": "User not found" }] }"""));

        var error = await Assert.ThrowsAsync<ImportException>(() => importer.ImportAsync("missing"));

        Assert.Contains("User not found", error.Message);
    }

    [Fact]
    public async Task Mal_importer_maps_anime_and_manga_rows()
    {
        var importer = new MalXmlImporter();

        var rows = await importer.ImportAsync(MalXml);

        Assert.Equal(2, rows.Count);
        var anime = rows[0];
        Assert.Equal("Cowboy Bebop", anime.Title);
        Assert.Equal(MediaKind.Anime, anime.Kind);
        Assert.Equal(MediaStatus.Rated, anime.Status);
        Assert.Equal(9, anime.Rating);
        Assert.Equal("26/26", anime.Progress);
        Assert.Equal("https://myanimelist.net/anime/1", anime.SourceUrl);
        Assert.Contains("mal", anime.Tags);

        var manga = rows[1];
        Assert.Equal("Berserk", manga.Title);
        Assert.Equal(MediaKind.Manga, manga.Kind);
        Assert.Equal(MediaStatus.Planned, manga.Status);
        Assert.Null(manga.Rating);
        Assert.Equal("10/100", manga.Progress);
    }

    [Fact]
    public async Task Mal_importer_rejects_invalid_xml()
    {
        var importer = new MalXmlImporter();

        await Assert.ThrowsAsync<ImportException>(() => importer.ImportAsync("<not-mal>"));
    }

    [Fact]
    public void Import_preview_deduplicates_by_normalized_title_and_kind()
    {
        var existing = new[]
        {
            new MediaItemBuilder().WithTitle("Spider-Man").WithKind(MediaKind.Film).Build(1),
        };
        var rows = new[]
        {
            new ImportedMedia("Spider Man", MediaKind.Film, "Film", [], MediaStatus.Planned, null, null, null, null, ""),
            new ImportedMedia("Spider Man", MediaKind.Anime, "TV", [], MediaStatus.Planned, null, null, null, null, ""),
        };
        var service = new ImportService();

        var preview = service.Preview(rows, existing);

        Assert.Single(preview.NewItems);
        Assert.Single(preview.Duplicates);
        Assert.Equal(MediaKind.Anime, preview.NewItems[0].Kind);
    }

    [Fact]
    public void Import_build_assigns_ids_and_keeps_remote_cover_source()
    {
        var preview = new ImportPreview(
            [new ImportedMedia("Berserk", MediaKind.Manga, "Manga", ["fantasy"], MediaStatus.Rated, 10,
                "10/100", "https://img.example/berserk.jpg", "https://anilist.co/manga/3", "CURRENT")],
            []);
        var service = new ImportService();
        var id = 41;

        var items = service.BuildItems(preview, () => id++);

        var item = Assert.Single(items);
        Assert.Equal(41, item.Id);
        Assert.Equal(MediaStatus.Rated, item.Status);
        Assert.Equal(10, item.Rating);
        Assert.Equal("https://img.example/berserk.jpg", item.CoverSourceUrl);
        Assert.Null(item.CoverPath);
        Assert.Equal("CURRENT", item.SourceStatus);
    }

    [Fact]
    public void Import_build_downgrades_a_rated_row_without_a_score()
    {
        var preview = new ImportPreview(
            [new ImportedMedia("No score", MediaKind.Anime, "TV", [], MediaStatus.Rated, null, null, null, null, "COMPLETED")],
            []);

        var item = Assert.Single(new ImportService().BuildItems(preview, () => 1));

        Assert.Equal(MediaStatus.Consumed, item.Status);
        Assert.Null(item.Rating);
    }
}
