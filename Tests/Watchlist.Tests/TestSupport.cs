using System.Net;
using System.Text;

namespace Watchlist.Tests;

internal sealed class TempDirectory : IDisposable
{
    public TempDirectory()
    {
        Path = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            "tmps-watchlist-tests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path);
    }

    public string Path { get; }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
        catch (IOException)
        {
            // Windows can hold a file briefly; the OS temp cleaner gets it.
        }
    }
}

internal sealed class StubHttpHandler(
    Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
{
    public int Calls { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        Calls++;
        return Task.FromResult(responder(request));
    }

    public static HttpClient Json(string json) =>
        new(new StubHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        }));
}

internal static class TestItems
{
    public static byte[] JpegBytes() =>
    [
        0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01,
    ];
}
