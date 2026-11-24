using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Watchlist.Core.CoverSearch;
using Watchlist.Core.CoverSearch.Adapters;
using Watchlist.Core.CoverSearch.Decorators;
using Watchlist.Core.CoverSearch.Images;
using Watchlist.Core.CoverSearch.Providers;
using Watchlist.Core.Domain;

namespace Watchlist.Tests;

public sealed class CoverPatternTests
{
    private sealed class FakeCatalog(params CoverCandidate[] hits) : ICoverCatalog
    {
        public int Calls { get; private set; }

        public Task<IReadOnlyList<CoverCandidate>> SearchAsync(
            string query,
            MediaKind kind,
            CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.FromResult<IReadOnlyList<CoverCandidate>>(hits);
        }
    }

    private sealed class FakeImage(byte[] bytes, string contentType = "image/jpeg") : ICoverImage
    {
        public int Calls { get; private set; }
        public TaskCompletionSource? Delay { get; set; }

        public async Task<CoverImageData> GetAsync(CancellationToken cancellationToken = default)
        {
            Calls++;
            if (Delay is not null)
            {
                await Delay.Task.WaitAsync(cancellationToken);
            }

            return new CoverImageData(bytes, contentType, "fake");
        }
    }

    [Fact]
    public async Task Wikimedia_adapter_maps_foreign_response_to_cover_candidates()
    {
        const string json = """
        {
          "query": {
            "pages": [
              {
                "title": "File:The Matrix poster.jpg",
                "imageinfo": [{
                  "thumburl": "https://thumb.example/matrix.jpg",
                  "url": "https://upload.example/matrix.jpg",
                  "mime": "image/jpeg"
                }]
              },
              {
                "title": "File:Ignored.svg",
                "imageinfo": [{ "thumburl": "https://thumb.example/x.svg", "url": "https://upload.example/x.svg", "mime": "image/svg+xml" }]
              }
            ]
          }
        }
        """;
        var adapter = new WikimediaCoverAdapter(new WikimediaCommonsClient(StubHttpHandler.Json(json)));

        var hits = await adapter.SearchAsync("matrix", MediaKind.Film);

        var hit = Assert.Single(hits);
        Assert.Equal("The Matrix poster", hit.Title);
        Assert.Equal("https://thumb.example/matrix.jpg", hit.ThumbnailUrl);
        Assert.Equal("Wikimedia Commons", hit.Provider);
    }

    [Fact]
    public async Task OpenLibrary_adapter_skips_books_without_a_cover_id()
    {
        const string json = """
        {
          "docs": [
            { "title": "Dune", "cover_i": 11481354, "author_name": ["Frank Herbert"] },
            { "title": "No cover", "author_name": ["Someone"] }
          ]
        }
        """;
        var adapter = new OpenLibraryCoverAdapter(new OpenLibraryClient(StubHttpHandler.Json(json)));

        var hits = await adapter.SearchAsync("dune", MediaKind.Book);

        var hit = Assert.Single(hits);
        Assert.Equal("Dune — Frank Herbert", hit.Title);
        Assert.Contains("11481354-L.jpg", hit.ImageUrl);
        Assert.Equal("Open Library", hit.Provider);
    }

    [Fact]
    public async Task Caching_decorator_avoids_a_second_provider_call()
    {
        var inner = new FakeCatalog(new CoverCandidate("A", "https://x/t.jpg", "https://x/i.jpg", "test"));
        var catalog = new CachingCoverCatalog(inner);

        var first = await catalog.SearchAsync("dune", MediaKind.Book);
        var second = await catalog.SearchAsync("DUNE", MediaKind.Book);

        Assert.Single(first);
        Assert.Single(second);
        Assert.Equal(1, inner.Calls);
    }

    [Fact]
    public async Task Filtering_decorator_removes_invalid_duplicate_and_svg_hits()
    {
        var inner = new FakeCatalog(
            new CoverCandidate("Good", "https://x/t1.jpg", "https://x/i1.jpg", "test"),
            new CoverCandidate("Duplicate", "https://x/t2.jpg", "https://x/i1.jpg", "test"),
            new CoverCandidate("SVG", "https://x/t3.svg", "https://x/i3.svg", "test"),
            new CoverCandidate("Bad scheme", "ftp://x/i4.jpg", "/relative/i5.jpg", "test"),
            new CoverCandidate("", "https://x/t6.jpg", "https://x/i6.jpg", "test"));
        var catalog = new FilteringCoverCatalog(inner);

        var hits = await catalog.SearchAsync("anything", MediaKind.Film);

        Assert.Equal("Good", Assert.Single(hits).Title);
    }

    [Fact]
    public async Task Decorators_can_be_stacked_in_either_order()
    {
        var inner = new FakeCatalog(
            new CoverCandidate("Good", "https://x/t.jpg", "https://x/i.jpg", "test"),
            new CoverCandidate("Bad", "https://x/t.svg", "https://x/i.svg", "test"));

        var filterThenCache = new CachingCoverCatalog(new FilteringCoverCatalog(inner));
        var cacheThenFilter = new FilteringCoverCatalog(new CachingCoverCatalog(inner));

        Assert.Equal(
            (await filterThenCache.SearchAsync("dune", MediaKind.Book)).Select(x => x.Title),
            (await cacheThenFilter.SearchAsync("dune", MediaKind.Book)).Select(x => x.Title));
    }

    [Fact]
    public async Task Proxy_downloads_once_then_serves_the_cached_file()
    {
        using var temp = new TempDirectory();
        var inner = new FakeImage(TestItems.JpegBytes());
        var path = Path.Combine(temp.Path, "covers", "0001.jpg");
        var proxy = new CachedCoverImageProxy(inner, path);

        var first = await proxy.GetAsync();
        var second = await proxy.GetAsync();

        Assert.Equal(1, inner.Calls);
        Assert.True(File.Exists(path));
        Assert.Equal(first.Bytes, second.Bytes);
        Assert.Equal("image/jpeg", second.ContentType);
    }

    [Fact]
    public async Task Proxy_rejects_a_payload_that_is_not_an_image()
    {
        using var temp = new TempDirectory();
        var proxy = new CachedCoverImageProxy(
            new FakeImage(Encoding.UTF8.GetBytes("hello"), "text/plain"),
            Path.Combine(temp.Path, "0001.jpg"));

        await Assert.ThrowsAsync<CoverImageException>(() => proxy.GetAsync());
        Assert.False(File.Exists(Path.Combine(temp.Path, "0001.jpg")));
    }

    [Fact]
    public async Task Proxy_serializes_concurrent_requests_for_the_same_cover()
    {
        using var temp = new TempDirectory();
        var inner = new FakeImage(TestItems.JpegBytes()) { Delay = new TaskCompletionSource() };
        var proxy = new CachedCoverImageProxy(inner, Path.Combine(temp.Path, "0001.jpg"));
        var first = proxy.GetAsync();
        var second = proxy.GetAsync();
        inner.Delay.SetResult();

        await Task.WhenAll(first, second);

        Assert.Equal(1, inner.Calls);
    }

    [Fact]
    public async Task Cover_service_uses_the_proxy_cache_for_a_second_save()
    {
        using var temp = new TempDirectory();
        var jpeg = TestItems.JpegBytes();
        var http = new HttpClient(new StubHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(jpeg),
        }));
        http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("image/*"));
        var service = new CoverService(
            new FakeCatalog(),
            http,
            Path.Combine(temp.Path, "covers"));
        var candidate = new CoverCandidate("A", "https://x/t.jpg", "https://x/i.jpg", "test");

        var first = await service.SaveAsync(1, candidate);
        var second = await service.SaveAsync(1, candidate);

        Assert.Equal("covers/0001.jpg", first.RelativePath);
        Assert.Equal(jpeg.Length, first.Bytes);
        Assert.Equal(jpeg.Length, second.Bytes);
        Assert.Single(Directory.GetFiles(Path.Combine(temp.Path, "covers"), "*.jpg"));
    }

    [Fact]
    public async Task Cover_service_downloads_the_provider_thumbnail_for_the_local_card()
    {
        using var temp = new TempDirectory();
        var seen = new List<string>();
        var http = new HttpClient(new StubHttpHandler(request =>
        {
            seen.Add(request.RequestUri!.ToString());
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(TestItems.JpegBytes()),
            };
        }));
        var service = new CoverService(new FakeCatalog(), http, Path.Combine(temp.Path, "covers"));
        var candidate = new CoverCandidate(
            "A",
            "https://x/thumb.jpg",
            "https://x/original.jpg",
            "test");

        await service.SaveAsync(1, candidate);

        Assert.Equal("https://x/thumb.jpg", Assert.Single(seen));
    }
}
