using Lab0;

var library = new InMemoryMediaLibrary();
var scoreCalculator = new RatingScoreCalculator();
var watchlist = new WatchlistService(library, scoreCalculator);
var app = new WatchlistConsoleApp(watchlist);

app.Run();
