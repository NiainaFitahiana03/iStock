using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IStock.Models;
using IStock.Services;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace IStock.ViewModels.Pages;

public partial class DashboardViewModel : ViewModelBase
{
    private readonly ProduitService _produitService;
    private readonly VenteService _venteService;

    public string Titre => "Tableau de bord";
    public string Description => "Vue d'ensemble de l'activité en temps réel.";

    // ===== Événement pour la navigation depuis le Dashboard =====
    public Action<string>? OnNavigate { get; set; }

    // ===== KPI Cards =====
    [ObservableProperty] private string _totalVentesJour = "0 Ar";
    [ObservableProperty] private string _nombreVentesJour = "0";
    [ObservableProperty] private string _produitsEnAlerte = "0";
    [ObservableProperty] private string _totalProduits = "0";

    // ===== Graphiques =====
    [ObservableProperty] private ISeries[] _seriesVentes;
    [ObservableProperty] private Axis[] _xAxesVentes;
    
    [ObservableProperty] private ISeries[] _seriesStock;
    [ObservableProperty] private Axis[] _xAxesStock;

    // ===== Activités récentes (Top 5 ventes) =====
    public System.Collections.ObjectModel.ObservableCollection<Vente> VentesRecentes { get; } = new();

    public DashboardViewModel(ProduitService produitService, VenteService venteService)
    {
        _produitService = produitService;
        _venteService = venteService;

        // Initialisation des graphiques vides pour éviter les NullReference
        _seriesVentes = Array.Empty<ISeries>();
        _xAxesVentes = Array.Empty<Axis>();
        _seriesStock = Array.Empty<ISeries>();
        _xAxesStock = Array.Empty<Axis>();

        _ = LoadDataAsync();
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        var produits = await _produitService.GetAllAsync();
        var ventes = await _venteService.GetToutesLesVentesAsync();

        // ===== 1. Calcul des KPIs =====
        var today = DateTime.Today;
        var ventesDuJour = ventes.Where(v => v.DateVente.Date == today).ToList();

        TotalVentesJour = $"{ventesDuJour.Sum(v => v.Total):N0} Ar";
        NombreVentesJour = ventesDuJour.Count.ToString();
        ProduitsEnAlerte = produits.Count(p => p.QuantiteStock <= p.SeuilAlerte).ToString();
        TotalProduits = produits.Count.ToString();

        // ===== 2. Activités récentes =====
        VentesRecentes.Clear();
        foreach (var v in ventes.Take(5))
            VentesRecentes.Add(v);

        // ===== 3. Graphique Ventes (7 derniers jours) =====
        var last7Days = Enumerable.Range(0, 7)
            .Select(i => today.AddDays(-i))
            .OrderBy(d => d)
            .ToList();

        var valeursVentes = last7Days.Select(d => 
            (double)ventes.Where(v => v.DateVente.Date == d).Sum(v => v.Total)).ToArray();

        SeriesVentes = new ISeries[]
        {
            new ColumnSeries<double>
            {
                Values = valeursVentes,
                Name = "Ventes (Ar)",
                Fill = new SolidColorPaint(SKColor.Parse("#2563EB")),
                MaxBarWidth = 40,
                DataLabelsPaint = new SolidColorPaint(SKColors.White),
                DataLabelsPosition = LiveChartsCore.Measure.DataLabelsPosition.Top
            }
        };

        XAxesVentes = new Axis[]
        {
            new Axis
            {
                Labels = last7Days.Select(d => d.ToString("dd/MM")).ToArray(),
                LabelsPaint = new SolidColorPaint(SKColor.Parse("#6B7280")),
                SeparatorsPaint = new SolidColorPaint(SKColor.Parse("#E5E7EB"))
            }
        };

        // ===== 4. Graphique Stock (Top 8 produits par quantité) =====
        var topProduits = produits.OrderByDescending(p => p.QuantiteStock).Take(8).ToList();
        
        SeriesStock = new ISeries[]
        {
            new RowSeries<double>
            {
                Values = topProduits.Select(p => (double)p.QuantiteStock).ToArray(),
                Name = "Quantité",
                Fill = new SolidColorPaint(SKColor.Parse("#16A34A")),
                MaxBarWidth = 20
            }
        };

        XAxesStock = new Axis[]
        {
            new Axis
            {
                Labels = topProduits.Select(p => p.Nom).ToArray(),
                LabelsPaint = new SolidColorPaint(SKColor.Parse("#6B7280")),
                SeparatorsPaint = new SolidColorPaint(SKColor.Parse("#E5E7EB"))
            }
        };
    }

    // ===== Commandes de raccourcis =====
    [RelayCommand]
    private void AllerVersVente() => OnNavigate?.Invoke("Vente");

    [RelayCommand]
    private void AllerVersStock() => OnNavigate?.Invoke("Stock");

    [RelayCommand]
    private void AllerVersHistorique() => OnNavigate?.Invoke("Historique");
}