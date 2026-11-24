using System.Text;
using Lab2;

Console.OutputEncoding = Encoding.UTF8;

var root = Environment.GetEnvironmentVariable("TMPS_LAB2_LIBRARY");
if (string.IsNullOrWhiteSpace(root))
{
    root = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "TMPS-Labs",
        "Lab2",
        "library");
}

var app = Lab2Composition.Create(root);
await app.RunAsync();
