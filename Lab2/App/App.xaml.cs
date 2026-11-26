namespace Lab2;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var services = AppServices.Create();
        return new Window(new NavigationPage(new MainPage(services))
        {
            BarBackgroundColor = Color.FromArgb("#16202A"),
            BarTextColor = Colors.White,
        })
        {
            Title = "Watchlist Lab 2",
        };
    }
}
