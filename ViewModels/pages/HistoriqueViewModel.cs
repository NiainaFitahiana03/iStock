using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IStock.Models;
using IStock.Services;

namespace IStock.ViewModels.Pages;

public partial class HistoriqueViewModel : ViewModelBase
{
    private readonly VenteService _venteService;
    private readonly HistoriqueService _historiqueService;

    public string Titre => "Historique";
    public string Description => "Toutes les transactions : ventes et mouvements de stock.";

    public ObservableCollection<Vente> Ventes { get; } = new();
    public ObservableCollection<MouvementStock> Mouvements { get; } = new();

    [ObservableProperty]
    private Vente? _venteSelectionnee;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private decimal _totalGeneral;

    [ObservableProperty]
    private int _nombreVentes;

    [ObservableProperty]
    private int _nombreMouvements;

    public HistoriqueViewModel(VenteService venteService, HistoriqueService historiqueService)
    {
        _venteService = venteService;
        _historiqueService = historiqueService;
        _ = LoadAsync();
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsLoading = true;

        Ventes.Clear();
        var ventes = await _venteService.GetToutesLesVentesAsync();
        foreach (var v in ventes) Ventes.Add(v);
        NombreVentes = Ventes.Count;
        TotalGeneral = Ventes.Sum(v => v.Total);

        Mouvements.Clear();
        var mouvements = await _historiqueService.GetMouvementsAsync();
        foreach (var m in mouvements) Mouvements.Add(m);
        NombreMouvements = Mouvements.Count;

        IsLoading = false;
    }
}