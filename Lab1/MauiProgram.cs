using Lab1.Factory.FactoryMethod;
using Lab1.Factory.Singleton;
using Microsoft.Extensions.Logging;

namespace Lab1;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        // Factory Method: pick the concrete library creator here.
        MediaLibraryFactory libraryFactory = new JsonLibraryFactory();

        // Singleton: the app shares exactly one library instance.
        var library = MediaLibraryStore.Instance.EnsureInitialized(
            libraryFactory.CreateLibrary());

        // The service depends only on the abstractions (DIP).
        builder.Services.AddSingleton(
            new Domain.Services.WatchlistService(library, new Domain.Scoring.RatingScoreCalculator()));

        return builder.Build();
    }
}
