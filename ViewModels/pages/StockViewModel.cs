using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IStock.Models;
using IStock.Services;

namespace IStock.ViewModels.Pages;

public partial class StockViewModel : ViewModelBase
{
    // ===== Services (déclarés UNE SEULE FOIS) =====
    private readonly ProduitService _produitService;
    private readonly ImportService _importService;
    private readonly AlerteService _alerteService;

    public string Titre => "Gestion du Stock";
    public string Description => "Catalogue des produits et niveaux d'inventaire.";

    // ===== Données brutes =====
    private readonly List<Produit> _tousLesProduits = new();

    // ===== Collection filtrée =====
    public ObservableCollection<Produit> Produits { get; } = new();

    // ===== Suggestions catégories =====
    public ObservableCollection<string> Categories { get; } = new();

    // ===== Recherche =====
    [ObservableProperty]
    private string _recherche = "";

    partial void OnRechercheChanged(string value) => AppliquerFiltre();

    // ===== Sélection =====
    [ObservableProperty]
    private Produit? _selectedProduit;

    // ===== Pop-up formulaire =====
    [ObservableProperty]
    private bool _isFormVisible;

    // ===== Champs formulaire =====
    [ObservableProperty]
    private string _formNom = "";

    [ObservableProperty]
    private string _formCategorie = "";

    [ObservableProperty]
    private decimal _formPrixUnitaire;

    [ObservableProperty]
    private int _formQuantiteStock;

    [ObservableProperty]
    private int _formSeuilAlerte = 5;

    [ObservableProperty]
    private bool _isEditing;

    // ===== État =====
    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = "";

    // ===== Constructeur (UNE SEULE FOIS) =====
    public StockViewModel(ProduitService produitService,
                          ImportService importService,
                          AlerteService alerteService)
    {
        _produitService = produitService;
        _importService = importService;
        _alerteService = alerteService;

        _ = LoadProduitsAsync();
    }

    // ===== Chargement =====
    [RelayCommand]
    private async Task LoadProduitsAsync()
    {
        IsLoading = true;

        _tousLesProduits.Clear();
        var list = await _produitService.GetAllAsync();
        _tousLesProduits.AddRange(list);

        // Mise à jour des catégories suggérées
        var cats = _tousLesProduits
            .Select(p => p.Categorie)
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Distinct()
            .OrderBy(c => c)
            .ToList();

        Categories.Clear();
        foreach (var c in cats)
            Categories.Add(c);

        AppliquerFiltre();
        IsLoading = false;
    }

    // ===== Filtre de recherche =====
    private void AppliquerFiltre()
    {
        Produits.Clear();

        var recherche = (Recherche ?? "").Trim().ToLowerInvariant();

        var resultat = string.IsNullOrEmpty(recherche)
            ? _tousLesProduits
            : _tousLesProduits.Where(p =>
                p.Nom.ToLowerInvariant().Contains(recherche) ||
                p.Categorie.ToLowerInvariant().Contains(recherche)).ToList();

        foreach (var p in resultat)
            Produits.Add(p);
    }

    // ===== Ouverture pop-up =====
    [RelayCommand]
    private void NouveauProduit()
    {
        ResetForm();
        IsEditing = false;
        IsFormVisible = true;
    }

    [RelayCommand]
    private void ModifierProduit()
    {
        if (SelectedProduit is null) return;

        FormNom = SelectedProduit.Nom;
        FormCategorie = SelectedProduit.Categorie;
        FormPrixUnitaire = SelectedProduit.PrixUnitaire;
        FormQuantiteStock = SelectedProduit.QuantiteStock;
        FormSeuilAlerte = SelectedProduit.SeuilAlerte;
        IsEditing = true;
        IsFormVisible = true;
    }

    [RelayCommand]
    private void FermerFormulaire()
    {
        IsFormVisible = false;
        ResetForm();
    }

    // ===== Enregistrement =====
    [RelayCommand]
    private async Task EnregistrerAsync()
    {
        if (string.IsNullOrWhiteSpace(FormNom))
        {
            StatusMessage = "Le nom du produit est obligatoire.";
            return;
        }

        if (string.IsNullOrWhiteSpace(FormCategorie))
        {
            StatusMessage = "La catégorie est obligatoire.";
            return;
        }

        if (FormPrixUnitaire < 0 || FormQuantiteStock < 0 || FormSeuilAlerte < 0)
        {
            StatusMessage = "Les valeurs numériques doivent être positives.";
            return;
        }

        if (IsEditing && SelectedProduit is not null)
        {
            await _produitService.MettreAJourAsync(new Produit
            {
                Id = SelectedProduit.Id,
                Nom = FormNom,
                Categorie = FormCategorie,
                PrixUnitaire = FormPrixUnitaire,
                QuantiteStock = FormQuantiteStock,
                SeuilAlerte = FormSeuilAlerte,
                Actif = true
            });
            StatusMessage = "Produit mis à jour.";
        }
        else
        {
            await _produitService.AjouterAsync(new Produit
            {
                Nom = FormNom,
                Categorie = FormCategorie,
                PrixUnitaire = FormPrixUnitaire,
                QuantiteStock = FormQuantiteStock,
                SeuilAlerte = FormSeuilAlerte
            });
            StatusMessage = "Produit ajouté.";
        }

        IsFormVisible = false;
        ResetForm();
        await LoadProduitsAsync();

        // Vérifier les seuils après modification
        await _alerteService.VerifierAsync();
    }

    // ===== Import =====
    [RelayCommand]
    private async Task ImporterAsync()
    {
        var files = await ShowOpenFileDialogAsync();
        if (files is null || files.Count == 0) return;

        var rapport = new System.Text.StringBuilder();
        foreach (var file in files)
        {
            var res = await _importService.ImporterAsync(file);
            if (!res.Succes)
            {
                rapport.AppendLine($"❌ {System.IO.Path.GetFileName(file)} : {res.ErreurGlobale}");
                continue;
            }
            rapport.AppendLine(
                $"✅ {System.IO.Path.GetFileName(file)} : {res.LignesImportees}/{res.TotalLignes} lignes importées.");
            foreach (var e in res.Erreurs.Take(5))
                rapport.AppendLine($"   • {e}");
            if (res.Erreurs.Count > 5)
                rapport.AppendLine($"   • … et {res.Erreurs.Count - 5} autre(s).");
        }

        StatusMessage = rapport.ToString();
        await LoadProduitsAsync();

        // Vérifier les seuils après import
        await _alerteService.VerifierAsync();
    }

    private async Task<IReadOnlyList<string>?> ShowOpenFileDialogAsync()
    {
        var topLevel = Avalonia.Application.Current?.ApplicationLifetime is
            Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow : null;
        if (topLevel is null) return null;

        var dialog = new FilePickerOpenOptions
        {
            Title = "Sélectionner un ou plusieurs fichiers à importer",
            AllowMultiple = true,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Classeur Excel (.xlsx)") { Patterns = new[] { "*.xlsx" } },
                new FilePickerFileType("Base SQLite (.db)") { Patterns = new[] { "*.db", "*.sqlite", "*.sqlite3" } }
            }
        };

        var result = await topLevel.StorageProvider.OpenFilePickerAsync(dialog);
        if (result is null || result.Count == 0) return null;

        return result.Select(f => f.Path.LocalPath).ToList();
    }

    // ===== Suppression =====
    [RelayCommand]
    private async Task SupprimerAsync()
    {
        if (SelectedProduit is null) return;
        await _produitService.SupprimerAsync(SelectedProduit.Id);
        StatusMessage = $"Produit « {SelectedProduit.Nom} » supprimé.";
        ResetForm();
        await LoadProduitsAsync();
    }

    // ===== Reset =====
    private void ResetForm()
    {
        FormNom = "";
        FormCategorie = "";
        FormPrixUnitaire = 0;
        FormQuantiteStock = 0;
        FormSeuilAlerte = 5;
    }
}