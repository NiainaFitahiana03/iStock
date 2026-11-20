using CommunityToolkit.Mvvm.ComponentModel;

namespace IStock.Models;

public partial class AppSettings : ObservableObject
{
    [ObservableProperty] private string _nomEntreprise = "Mon Entreprise";
    [ObservableProperty] private string _devise = "Ar";
    [ObservableProperty] private int _seuilAlerteDefaut = 5;
    [ObservableProperty] private bool _autoriserStockNegatif = false;
    [ObservableProperty] private string _theme = "Système"; // "Clair", "Sombre", "Système"
}