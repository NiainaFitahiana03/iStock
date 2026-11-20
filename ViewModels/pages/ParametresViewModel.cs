using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace IStock.ViewModels.Pages;

public partial class ParamCategory : ObservableObject
{
    public string Icon { get; }
    public string Title { get; }
    public string Keywords { get; }
    
    [ObservableProperty] private bool _isVisible = true;

    public ParamCategory(string icon, string title, string keywords)
    {
        Icon = icon;
        Title = title;
        Keywords = keywords;
    }
}

public partial class ParametresViewModel : ViewModelBase
{
    public string Titre => "Paramètres";
    public string Description => "Configuration de l'application.";

    // ===== RECHERCHE & MENU =====
    [ObservableProperty] private string _recherche = "";
    partial void OnRechercheChanged(string value) => FiltrerCategories();

    public ObservableCollection<ParamCategory> Categories { get; } = new();
    
    [ObservableProperty] private ParamCategory? _selectedCategory;
    
    [ObservableProperty] private string _sectionCouranteTitre = "";
    [ObservableProperty] private string _sectionCouranteIcone = "";

    // ===== ÉTAT DE LA SECTION COURANTE (pour le XAML) =====
    public bool IsEntrepriseVisible => SelectedCategory?.Title == "Entreprise & Fiscalité";
    public bool IsStockVisible => SelectedCategory?.Title == "Stock & Inventaire";
    public bool IsVentesVisible => SelectedCategory?.Title == "Ventes & Caisse";
    public bool IsMaterielVisible => SelectedCategory?.Title == "Matériel & Périphériques";
    public bool IsUtilisateursVisible => SelectedCategory?.Title == "Utilisateurs & Rôles";
    public bool IsDonneesVisible => SelectedCategory?.Title == "Données & Sauvegarde";
    public bool IsInterfaceVisible => SelectedCategory?.Title == "Interface & Système";

    // ===== BARRE D'ACTIONS FIXE =====
    [ObservableProperty] private bool _hasUnsavedChanges = false;
    partial void OnHasUnsavedChangesChanged(bool value) => UpdateStatusMessage();
    
    [ObservableProperty] private string _statusMessage = "🟢 Toutes les modifications sont enregistrées";

    // ================= DONNÉES DES SECTIONS =================
    
    // --- 1. Entreprise ---
    [ObservableProperty] private string _nomCommercial = "Quincaillerie & Matériaux du Centre";
    [ObservableProperty] private string _raisonSociale = "QMC SARL";
    [ObservableProperty] private string _adresse = "Lot II M 45 Ter, Rue Rainandrianampandry";
    [ObservableProperty] private string _contact = "+261 34 00 123 45";
    [ObservableProperty] private string _nif = "4001234567";
    [ObservableProperty] private string _deviseSymbole = "Ar";
    [ObservableProperty] private decimal _tauxTva = 20.00m;
    [ObservableProperty] private bool _assujettiTva = true;

    // --- 2. Stock ---
    [ObservableProperty] private int _seuilAlerteDefaut = 5;
    [ObservableProperty] private bool _autoriserStockNegatif = false;
    [ObservableProperty] private bool _avertirStockInsuffisant = true;
    [ObservableProperty] private string _methodeValorisation = "CUMP";
    
    // --- 3. Ventes ---
    [ObservableProperty] private decimal _remiseMax = 5.00m;
    [ObservableProperty] private bool _autoriserVenteCredit = true;
    [ObservableProperty] private decimal _plafondCreditDefaut = 500000m;
    [ObservableProperty] private bool _paiementMixte = true;

    // --- 4. Matériel ---
    [ObservableProperty] private string _imprimanteTicket = "EPSON TM-T20III (USB)";
    [ObservableProperty] private string _largeurPapier = "80 mm";
    [ObservableProperty] private bool _tiroirAutoEspeces = true;

    // --- 5. Utilisateurs ---
    [ObservableProperty] private string _dureeVerrouillage = "Jamais";

    // --- 6. Données ---
    [ObservableProperty] private bool _sauvegardeAutoFermeture = true;
    [ObservableProperty] private string _dossierSauvegarde = @"D:\Sauvegardes_StockVente\";
    [ObservableProperty] private bool _compresserSauvegardes = true;

    // --- 7. Interface ---
    [ObservableProperty] private string _theme = "Système";
    [ObservableProperty] private string _taillePolice = "100 % (Standard)";

    // ================= CONSTRUCTEUR =================
    public ParametresViewModel()
    {
        Categories.Add(new ParamCategory("🏢", "Entreprise & Fiscalité", "nom adresse nif stat devise tva facture"));
        Categories.Add(new ParamCategory("📦", "Stock & Inventaire", "décrémentation valorisation cump fifo seuil alerte code-barres"));
        Categories.Add(new ParamCategory("🛒", "Ventes & Caisse", "remise crédit acompte paiement espèces mobile money client"));
        Categories.Add(new ParamCategory("🖨️", "Matériel & Périphériques", "imprimante ticket thermique scanner code-barres tiroir caisse"));
        Categories.Add(new ParamCategory("👥", "Utilisateurs & Rôles", "compte opérateur caissier gestionnaire admin permission accès"));
        Categories.Add(new ParamCategory("💾", "Données & Sauvegarde", "sqlite chemin backup restauration import export csv audit"));
        Categories.Add(new ParamCategory("🎨", "Interface & Système", "thème sombre clair police raccourci démarrage version mise à jour"));

        SelectedCategory = Categories.FirstOrDefault();
    }

    // ================= LOGIQUE =================
    
    partial void OnSelectedCategoryChanged(ParamCategory? value)
    {
        if (value == null) return;
        
        SectionCouranteIcone = value.Icon;
        SectionCouranteTitre = value.Title;

        // Notification pour forcer le XAML à mettre à jour l'affichage
        OnPropertyChanged(nameof(IsEntrepriseVisible));
        OnPropertyChanged(nameof(IsStockVisible));
        OnPropertyChanged(nameof(IsVentesVisible));
        OnPropertyChanged(nameof(IsMaterielVisible));
        OnPropertyChanged(nameof(IsUtilisateursVisible));
        OnPropertyChanged(nameof(IsDonneesVisible));
        OnPropertyChanged(nameof(IsInterfaceVisible));
    }

    private void FiltrerCategories()
    {
        var q = (Recherche ?? "").Trim().ToLowerInvariant();

        foreach (var cat in Categories)
        {
            cat.IsVisible = string.IsNullOrEmpty(q) || 
                            cat.Title.ToLowerInvariant().Contains(q) || 
                            cat.Keywords.Contains(q);
        }

        if (SelectedCategory != null && !SelectedCategory.IsVisible)
        {
            SelectedCategory = Categories.FirstOrDefault(c => c.IsVisible);
        }
    }

    private void UpdateStatusMessage()
    {
        if (HasUnsavedChanges)
            StatusMessage = "🟠 Modifications non enregistrées...";
        else
            StatusMessage = "🟢 Toutes les modifications sont enregistrées";
    }

    // ================= COMMANDES =================
    
    [RelayCommand]
    private void Enregistrer()
    {
        HasUnsavedChanges = false;
    }

    [RelayCommand]
    private void Annuler()
    {
        HasUnsavedChanges = false;
    }

    [RelayCommand]
    private void ReinitialiserSection()
    {
        HasUnsavedChanges = true;
    }
}