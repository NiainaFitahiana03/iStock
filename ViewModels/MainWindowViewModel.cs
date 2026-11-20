using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IStock.Models;
using IStock.Services;
using IStock.ViewModels.Pages;

namespace IStock.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly ProduitService _produitService;
    private readonly VenteService _venteService;
    private readonly HistoriqueService _historiqueService;
    private readonly ImportService _importService;
    private readonly NotificationService _notificationService;
    private readonly AlerteService _alerteService;

    public ObservableCollection<MenuItem> MenuItems { get; }

    [ObservableProperty]
    private MenuItem? _selectedMenuItem;

    [ObservableProperty]
    private ViewModelBase? _currentPage;

    // ===== État du popup de notifications =====
    [ObservableProperty]
    private bool _isNotificationsOpen;

    // ===== État du popup utilisateur =====
    [ObservableProperty]
    private bool _isUserMenuOpen;

    // ===== Exposer le service pour le binding =====
    public NotificationService Notifications => _notificationService;

    public MainWindowViewModel(
        ProduitService produitService,
        VenteService venteService,
        HistoriqueService historiqueService,
        ImportService importService,
        NotificationService notificationService,
        AlerteService alerteService)
    {
        _produitService = produitService;
        _venteService = venteService;
        _historiqueService = historiqueService;
        _importService = importService;
        _notificationService = notificationService;
        _alerteService = alerteService;

        // Initialisation des pages avec injection des services
        MenuItems = new ObservableCollection<MenuItem>
        {
            new("Dashboard", () => 
            {
                var vm = new DashboardViewModel(_produitService, _venteService);
                vm.OnNavigate = NaviguerVers;
                return vm;
            }),
            new("Stock", () => new StockViewModel(_produitService, _importService, _alerteService)),
            new("Vente", () => new VenteViewModel(_produitService, _venteService, _alerteService))
        };

        // Sélectionne le Dashboard par défaut
        SelectedMenuItem = MenuItems[0];
    }

    partial void OnSelectedMenuItemChanged(MenuItem? value)
    {
        if (value is not null)
            CurrentPage = value.CreatePage();
    }

    // ===== Navigation programmatique =====
    private void NaviguerVers(string destination)
    {
        if (destination == "Historique")
        {
            OuvrirHistorique();
            return;
        }

        var item = MenuItems.FirstOrDefault(m => m.Title == destination);
        if (item is not null)
            SelectedMenuItem = item;
    }

    // ===== Commandes du menu latéral =====
    [RelayCommand]
    private void OuvrirHistorique() =>
        CurrentPage = new HistoriqueViewModel(_venteService, _historiqueService);

    // ===== Commandes des notifications =====
    [RelayCommand]
    private void ToggleNotifications() =>
        IsNotificationsOpen = !IsNotificationsOpen;

    [RelayCommand]
    private void MarquerToutLu()
    {
        _notificationService.MarquerToutCommeLue();
    }

    [RelayCommand]
    private void FermerToast(Notification notif) =>
        _notificationService.FermerToast(notif);

    // ===== Commandes du menu utilisateur =====
    [RelayCommand]
    private void ToggleUserMenu() => IsUserMenuOpen = !IsUserMenuOpen;

    [RelayCommand]
    private void Aide()
    {
        IsUserMenuOpen = false;
        CurrentPage = new AideViewModel();
    }

    [RelayCommand]
    private void Parametres()
    {
        IsUserMenuOpen = false;
        // TODO: Remplacer par une vraie page Paramètres quand elle sera créée
        CurrentPage = new ParametresViewModel(); 
    }

    [RelayCommand]
    private void Quitter()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime
            is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    }
}