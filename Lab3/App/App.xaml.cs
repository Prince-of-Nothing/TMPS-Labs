namespace Lab3;

public partial class App : Application
{
    private readonly AppServices _services;

    public App()
    {
        InitializeComponent();
        _services = AppServices.Create();
    }

    protected override Window CreateWindow(IActivationState? activationState) =>
        new(new NavigationPage(new MainPage(_services)))
        {
            Title = "Watchlist",
        };
}
