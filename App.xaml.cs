using NevApps.Services;

namespace NevApps;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
        new StateService().ApplyToApplication();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new MainPage()) { Title = "NevApps" };
    }
}