using Velopack;

namespace Cruma.Desktop;

public static class Program
{
    /// <summary>Vstupní bod: Velopack musí proběhnout jako první (instalace, aktualizace, odinstalace).</summary>
    [STAThread]
    public static void Main(string[] args)
    {
        VelopackApp.Build().Run();

        var app = new App();
        app.InitializeComponent();
        app.Run();
    }
}
