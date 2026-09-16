using System.Windows;

namespace Cruma.Desktop;

public partial class MainWindow : Window
{
    public MainWindow(IServiceProvider services, CrumaAppData appData)
    {
        InitializeComponent();
        WebView.Services = services;
        // Vývojový build je poznat v titulku (PER-006).
        Title = appData.IsDevelopmentBuild ? "Cruma (dev)" : "Cruma";
    }
}
