using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using IStock.Models;

namespace IStock.Services;

public partial class NotificationService : ObservableObject
{
    public ObservableCollection<Notification> Toutes { get; } = new();
    public ObservableCollection<Notification> Toasts { get; } = new();

    [ObservableProperty]
    private int _nombreNonLues;

    public NotificationService()
    {
        Toutes.CollectionChanged += (_, _) => MettreAJourCompteur();
    }

    // ===== Ajouter (sans doublon) =====
    // afficherToast = true uniquement lors d'une nouvelle alerte, pas lors d'une vérification globale
    public void Ajouter(string titre, string message, NotificationNiveau niveau = NotificationNiveau.Info, bool afficherToast = true)
    {
        // Vérifier si une notification identique (même titre + même message) existe déjà
        var existante = Toutes.FirstOrDefault(n => n.Titre == titre && n.Message == message);
        if (existante is not null)
        {
            // Si elle existe déjà, on ne recrée pas de toast
            return;
        }

        var notif = new Notification
        {
            Titre = titre,
            Message = message,
            Niveau = niveau
        };

        Toutes.Insert(0, notif);

        // N'afficher le toast que si demandé
        if (afficherToast)
        {
            Toasts.Add(notif);
            _ = AutoFermerToastAsync(notif);
        }
    }

    private async System.Threading.Tasks.Task AutoFermerToastAsync(Notification notif)
    {
        await System.Threading.Tasks.Task.Delay(5000);
        FermerToast(notif);
    }

    public void FermerToast(Notification notif)
    {
        if (Toasts.Contains(notif))
            Toasts.Remove(notif);
    }

    // ===== Nettoyer les alertes obsolètes =====
    public void NettoyerAlertesObsoletes(IEnumerable<string> produitsEncoreEnAlerte)
    {
        var aSupprimer = Toutes
            .Where(n => n.Titre == "Seuil d'alerte atteint" || n.Titre == "Rupture de stock")
            .Where(n => !produitsEncoreEnAlerte.Any(p => n.Message.Contains($"« {p} »")))
            .ToList();

        foreach (var n in aSupprimer)
            Toutes.Remove(n);
    }

    public void MarquerToutCommeLue()
    {
        foreach (var n in Toutes)
            n.EstLue = true;
        MettreAJourCompteur();
    }

    private void MettreAJourCompteur() =>
        NombreNonLues = Toutes.Count(n => !n.EstLue);
}