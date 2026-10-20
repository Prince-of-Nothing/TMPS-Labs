using Lab0;

var library = new JsonMediaLibrary();
Console.WriteLine($"Watchlist data file: {library.FilePath}");
var scoreCalculator = new RatingScoreCalculator();
var watchlist = new WatchlistService(library, scoreCalculator);
var app = new WatchlistConsoleApp(watchlist);

app.Run();
