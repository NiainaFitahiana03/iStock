using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IStock.Models;
using IStock.Services;

namespace IStock.ViewModels.Pages;

public partial class VenteViewModel : ViewModelBase
{
    private readonly ProduitService _produitService;
    private readonly VenteService _venteService;
    private readonly AlerteService _alerteService;

    public string Titre => "Ventes";
    public string Description => "Sélectionnez des produits pour les ajouter au panier.";

    // ===== Produits disponibles =====
    public ObservableCollection<Produit> ProduitsDisponibles { get; } = new();

    // ===== Panier =====
    public ObservableCollection<PanierItem> Panier { get; } = new();

    // ===== Visibilité =====
    [ObservableProperty]
    private bool _isPanierVisible = true;

    [ObservableProperty]
    private bool _isCalculatorVisible;

    // ===== Total =====
    [ObservableProperty]
    private decimal _totalPanier;

    // ===== Sélection =====
    [ObservableProperty]
    private Produit? _produitSelectionne;

    [ObservableProperty]
    private int _quantiteAAjouter = 1;

    // ===== Statut =====
    [ObservableProperty]
    private string _statusMessage = "";

    // ===== Calculatrice =====
    [ObservableProperty]
    private string _display = "0";

    private double? _operand;
    private char? _pendingOperator;
    private bool _resetOnNextDigit = false;

    // ===== Constructeur =====
    public VenteViewModel(ProduitService produitService, VenteService venteService, AlerteService alerteService)
    {
        _produitService = produitService;
        _venteService = venteService;
        _alerteService = alerteService;

        Panier.CollectionChanged += OnPanierChanged;
        _ = LoadProduitsAsync();
    }

    // ===== Chargement des produits =====
    [RelayCommand]
    private async Task LoadProduitsAsync()
    {
        ProduitsDisponibles.Clear();
        var list = await _produitService.GetAllAsync();
        foreach (var p in list)
            ProduitsDisponibles.Add(p);
    }

    // ===== Ajouter au panier =====
    [RelayCommand]
    private void AjouterAuPanier()
    {
        if (ProduitSelectionne is null)
        {
            StatusMessage = "Sélectionnez un produit dans la liste.";
            return;
        }

        if (QuantiteAAjouter <= 0)
        {
            StatusMessage = "La quantité doit être supérieure à zéro.";
            return;
        }

        if (ProduitSelectionne.QuantiteStock < QuantiteAAjouter)
        {
            StatusMessage = $"Stock insuffisant (disponible : {ProduitSelectionne.QuantiteStock}).";
            return;
        }

        // Avertissement si la vente fait passer sous le seuil
        var futurStock = ProduitSelectionne.QuantiteStock - QuantiteAAjouter;
        if (futurStock <= ProduitSelectionne.SeuilAlerte)
        {
            StatusMessage =
                $"⚠️ Attention : après cette vente, le stock de « {ProduitSelectionne.Nom} » " +
                $"sera de {futurStock} (seuil : {ProduitSelectionne.SeuilAlerte}).";
        }
        else
        {
            StatusMessage = "";
        }

        // Ajout / incrémentation dans le panier
        var existing = Panier.FirstOrDefault(p => p.ProduitId == ProduitSelectionne.Id);
        if (existing is not null)
            existing.Quantite += QuantiteAAjouter;
        else
            Panier.Add(new PanierItem(
                ProduitSelectionne.Id,
                ProduitSelectionne.Nom,
                ProduitSelectionne.PrixUnitaire,
                QuantiteAAjouter));

        QuantiteAAjouter = 1;
    }

    // ===== Retirer du panier =====
    [RelayCommand]
    private void RetirerDuPanier(PanierItem item)
    {
        if (item is null) return;
        Panier.Remove(item);
    }

    // ===== Gestion du panier =====
    private void OnPanierChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
            foreach (PanierItem item in e.OldItems)
                item.PropertyChanged -= OnPanierItemChanged;

        if (e.NewItems is not null)
            foreach (PanierItem item in e.NewItems)
                item.PropertyChanged += OnPanierItemChanged;

        RecalculateTotal();
    }

    private void OnPanierItemChanged(object? sender, PropertyChangedEventArgs e) => RecalculateTotal();

    private void RecalculateTotal() => TotalPanier = Panier.Sum(p => p.Prix * p.Quantite);

    // ===== Visibilité =====
    [RelayCommand]
    private void TogglePanier() => IsPanierVisible = !IsPanierVisible;

    [RelayCommand]
    private void ToggleCalculator() => IsCalculatorVisible = !IsCalculatorVisible;

    // ===== Valider la vente =====
    [RelayCommand]
private async Task ValiderAsync()
{
    if (Panier.Count == 0)
    {
        StatusMessage = "Le panier est vide.";
        return;
    }

    // Sauvegarder les ID des produits vendus avant de vider le panier
    var produitsVendusIds = Panier.Select(p => p.ProduitId).ToList();

    var panierMapped = Panier.Select(p => (
        new Produit
        {
            Id = p.ProduitId,
            Nom = p.Nom,
            PrixUnitaire = p.Prix
        },
        p.Quantite
    )).ToList();

    await _venteService.EnregistrerVenteAsync(panierMapped);

    StatusMessage = "Vente validée et enregistrée dans l'historique.";
    Panier.Clear();
    TotalPanier = 0;

    // Recharger les stocks à jour
    await LoadProduitsAsync();

    // Vérifier UNIQUEMENT les produits qui viennent d'être vendus
    foreach (var p in ProduitsDisponibles.Where(p => produitsVendusIds.Contains(p.Id)))
        _alerteService.VerifierProduit(p);
}

    // ===== Calculatrice =====
    [RelayCommand]
    private void PressDigit(string digit)
    {
        if (_resetOnNextDigit || Display == "0")
        {
            Display = digit;
            _resetOnNextDigit = false;
        }
        else
        {
            Display += digit;
        }
    }

    [RelayCommand]
    private void PressOperator(string op)
    {
        if (_pendingOperator is not null && !_resetOnNextDigit)
            Compute();

        _operand = ParseDisplay();
        _pendingOperator = op[0];
        _resetOnNextDigit = true;
    }

    [RelayCommand]
    private void PressEquals()
    {
        if (_pendingOperator is not null)
        {
            Compute();
            _pendingOperator = null;
            _resetOnNextDigit = true;
        }
    }

    [RelayCommand]
    private void Clear()
    {
        Display = "0";
        _operand = null;
        _pendingOperator = null;
        _resetOnNextDigit = false;
    }

    [RelayCommand]
    private void Backspace()
    {
        if (Display.Length <= 1)
            Display = "0";
        else
            Display = Display[..^1];
    }

    [RelayCommand]
    private void Decimal()
    {
        if (_resetOnNextDigit)
        {
            Display = "0.";
            _resetOnNextDigit = false;
        }
        else if (!Display.Contains('.'))
        {
            Display += ".";
        }
    }

    private double ParseDisplay() =>
        double.TryParse(Display, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : 0;

    private void Compute()
    {
        var current = ParseDisplay();
        var a = _operand ?? 0;

        double result = _pendingOperator switch
        {
            '+' => a + current,
            '-' => a - current,
            '*' => a * current,
            '/' => current == 0 ? double.NaN : a / current,
            _ => current
        };

        Display = double.IsNaN(result)
            ? "Erreur"
            : result.ToString("G", CultureInfo.InvariantCulture);

        _operand = result;
    }
}

// ===== PanierItem observable =====
public partial class PanierItem : ObservableObject
{
    [ObservableProperty]
    private int _produitId;

    [ObservableProperty]
    private string _nom = "";

    [ObservableProperty]
    private decimal _prix;

    [ObservableProperty]
    private int _quantite;

    public PanierItem(int produitId, string nom, decimal prix, int quantite)
    {
        _produitId = produitId;
        _nom = nom;
        _prix = prix;
        _quantite = quantite;
    }
}