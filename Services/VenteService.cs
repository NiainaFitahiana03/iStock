using IStock.Data;
using IStock.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace IStock.Services;

public class VenteService
{
    private readonly DatabaseService _db;
    private readonly ProduitService _produits;

    public VenteService(DatabaseService db, ProduitService produits)
    {
        _db = db;
        _produits = produits;
    }

    public async Task<int> EnregistrerVenteAsync(List<(Produit Produit, int Quantite)> panier)
    {
        if (panier.Count == 0) return 0;

        var total = panier.Sum(p => p.Produit.PrixUnitaire * p.Quantite);
        var dateStr = DateTime.Now.ToString("O", CultureInfo.InvariantCulture);

        await using var conn = _db.CreateHistoriqueConnection();
        await conn.OpenAsync();
        await using var tx = conn.BeginTransaction();

        try
        {
            using var cmdVente = conn.CreateCommand();
            cmdVente.Transaction = tx;
            cmdVente.CommandText = @"
                INSERT INTO Ventes (DateVente, Total) VALUES ($date, $total);
                SELECT last_insert_rowid();";
            cmdVente.Parameters.AddWithValue("$date", dateStr);
            cmdVente.Parameters.AddWithValue("$total", (double)total);
            var venteId = (int)(long)cmdVente.ExecuteScalar()!;

            foreach (var item in panier)
            {
                using var cmdDetail = conn.CreateCommand();
                cmdDetail.Transaction = tx;
                cmdDetail.CommandText = @"
                    INSERT INTO VenteDetails (VenteId, ProduitId, NomProduit, PrixUnitaire, Quantite)
                    VALUES ($vid, $pid, $nom, $prix, $qte)";
                cmdDetail.Parameters.AddWithValue("$vid", venteId);
                cmdDetail.Parameters.AddWithValue("$pid", item.Produit.Id);
                cmdDetail.Parameters.AddWithValue("$nom", item.Produit.Nom);
                cmdDetail.Parameters.AddWithValue("$prix", (double)item.Produit.PrixUnitaire);
                cmdDetail.Parameters.AddWithValue("$qte", item.Quantite);
                await cmdDetail.ExecuteNonQueryAsync();

                using var cmdMouvement = conn.CreateCommand();
                cmdMouvement.Transaction = tx;
                cmdMouvement.CommandText = @"
                    INSERT INTO MouvementsStock (ProduitId, Type, Quantite, DateMouvement, Raison)
                    VALUES ($pid, 'sortie', $qte, $date, 'vente')";
                cmdMouvement.Parameters.AddWithValue("$pid", item.Produit.Id);
                cmdMouvement.Parameters.AddWithValue("$qte", item.Quantite);
                cmdMouvement.Parameters.AddWithValue("$date", dateStr);
                await cmdMouvement.ExecuteNonQueryAsync();

                await _produits.MettreAJourStockAsync(item.Produit.Id, -item.Quantite);
            }

            await tx.CommitAsync();
            return venteId;
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task<List<Vente>> GetVentesDuJourAsync()
    {
        var list = new List<Vente>();
        var today = DateTime.Today.ToString("yyyy-MM-dd");
        await using var conn = _db.CreateHistoriqueConnection();
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT Id, DateVente, Total, Statut FROM Ventes
            WHERE DateVente LIKE $today || '%' ORDER BY DateVente DESC";
        cmd.Parameters.AddWithValue("$today", today);
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(new Vente
            {
                Id = reader.GetInt32(0),
                DateVente = DateTime.Parse(reader.GetString(1)),
                Total = (decimal)reader.GetDouble(2),
                Statut = reader.GetString(3)
            });
        }
        return list;
    }
    public async Task<List<Vente>> GetToutesLesVentesAsync()
{
    var list = new List<Vente>();
    await using var conn = _db.CreateHistoriqueConnection();
    await conn.OpenAsync();

    // 1. Toutes les ventes
    using (var cmd = conn.CreateCommand())
    {
        cmd.CommandText = @"SELECT Id, DateVente, Total, Statut 
                            FROM Ventes 
                            ORDER BY DateVente DESC";
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(new Vente
            {
                Id = reader.GetInt32(0),
                DateVente = DateTime.Parse(reader.GetString(1)),
                Total = (decimal)reader.GetDouble(2),
                Statut = reader.GetString(3)
            });
        }
    }

    // 2. Détails de chaque vente
    foreach (var vente in list)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"SELECT Id, ProduitId, NomProduit, PrixUnitaire, Quantite
                            FROM VenteDetails WHERE VenteId = $vid";
        cmd.Parameters.AddWithValue("$vid", vente.Id);
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            vente.Details.Add(new VenteDetail
            {
                Id = reader.GetInt32(0),
                VenteId = vente.Id,
                ProduitId = reader.GetInt32(1),
                NomProduit = reader.GetString(2),
                PrixUnitaire = (decimal)reader.GetDouble(3),
                Quantite = reader.GetInt32(4)
            });
        }
    }

    return list;
}
}