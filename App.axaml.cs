using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using IStock.Data;
using IStock.Services;
using IStock.ViewModels;
using IStock.Views;

namespace IStock;

public partial class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var db = new DatabaseService();
            var produitService = new ProduitService(db);
            var venteService = new VenteService(db, produitService);
            var historiqueService = new HistoriqueService(db);
            var importService = new ImportService(produitService);
            var notificationService = new NotificationService();
            var alerteService = new AlerteService(produitService, notificationService);

            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(
                    produitService, venteService, historiqueService,
                    importService, notificationService, alerteService)
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}