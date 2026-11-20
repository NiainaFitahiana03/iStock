using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IStock.Models;

namespace IStock.Services;

public class AlerteService
{
    private readonly ProduitService _produitService;
    private readonly NotificationService _notificationService;

    public AlerteService(ProduitService produitService, NotificationService notificationService)
    {
        _produitService = produitService;
        _notificationService = notificationService;
    }

    // ===== Vérification globale (ex: au démarrage, ou après import) =====
    public async Task VerifierAsync()
    {
        var produits = await _produitService.GetAllAsync();
        var nomsEnAlerte = new List<string>();

        foreach (var p in produits)
        {
            if (p.QuantiteStock == 0)
            {
                nomsEnAlerte.Add(p.Nom);
                _notificationService.Ajouter(
                    "Rupture de stock",
                    $"« {p.Nom} » est en rupture totale.",
                    NotificationNiveau.Critique,
                    afficherToast: false); // pas de toast à la vérification globale
            }
            else if (p.QuantiteStock <= p.SeuilAlerte)
            {
                nomsEnAlerte.Add(p.Nom);
                _notificationService.Ajouter(
                    "Seuil d'alerte atteint",
                    $"« {p.Nom} » : stock = {p.QuantiteStock} (seuil = {p.SeuilAlerte}).",
                    NotificationNiveau.Avertissement,
                    afficherToast: false); // pas de toast à la vérification globale
            }
        }

        // Supprimer les notifications des produits qui ne sont plus en alerte
        _notificationService.NettoyerAlertesObsoletes(nomsEnAlerte);
    }

    // ===== Vérification ciblée après une action (vente) =====
    public void VerifierProduit(Produit produit)
    {
        if (produit.QuantiteStock == 0)
        {
            _notificationService.Ajouter(
                "Rupture de stock",
                $"« {produit.Nom} » est en rupture totale.",
                NotificationNiveau.Critique,
                afficherToast: true); // on affiche le toast
        }
        else if (produit.QuantiteStock <= produit.SeuilAlerte)
        {
            _notificationService.Ajouter(
                "Seuil d'alerte atteint",
                $"« {produit.Nom} » : stock = {produit.QuantiteStock} (seuil = {produit.SeuilAlerte}).",
                NotificationNiveau.Avertissement,
                afficherToast: true); // on affiche le toast
        }
    }
}