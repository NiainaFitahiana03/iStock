using IStock.Data;
using IStock.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IStock.Services;

public class ProduitService
{
    private readonly DatabaseService _db;

    public ProduitService(DatabaseService db) => _db = db;

    public async Task<List<Produit>> GetAllAsync()
    {
        var list = new List<Produit>();
        await using var conn = _db.CreateProduitsConnection();
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"SELECT Id, Nom, Categorie, PrixUnitaire, QuantiteStock, SeuilAlerte, Actif
                            FROM Produits WHERE Actif = 1 ORDER BY Nom";
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(new Produit
            {
                Id = reader.GetInt32(0),
                Nom = reader.GetString(1),
                Categorie = reader.GetString(2),
                PrixUnitaire = (decimal)reader.GetDouble(3),
                QuantiteStock = reader.GetInt32(4),
                SeuilAlerte = reader.GetInt32(5),
                Actif = reader.GetInt32(6) == 1
            });
        }
        return list;
    }

    public async Task<int> AjouterAsync(Produit p)
    {
        await using var conn = _db.CreateProduitsConnection();
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO Produits (Nom, Categorie, PrixUnitaire, QuantiteStock, SeuilAlerte)
            VALUES ($nom, $cat, $prix, $qte, $seuil);
            SELECT last_insert_rowid();";
        cmd.Parameters.AddWithValue("$nom", p.Nom);
        cmd.Parameters.AddWithValue("$cat", p.Categorie);
        cmd.Parameters.AddWithValue("$prix", (double)p.PrixUnitaire);
        cmd.Parameters.AddWithValue("$qte", p.QuantiteStock);
        cmd.Parameters.AddWithValue("$seuil", p.SeuilAlerte);
        return (int)(long)cmd.ExecuteScalar()!;
    }

    public async Task MettreAJourStockAsync(int produitId, int delta)
    {
        await using var conn = _db.CreateProduitsConnection();
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE Produits SET QuantiteStock = QuantiteStock + $delta WHERE Id = $id";
        cmd.Parameters.AddWithValue("$delta", delta);
        cmd.Parameters.AddWithValue("$id", produitId);
        await cmd.ExecuteNonQueryAsync();
    }

    // ===== NOUVELLES MÉTHODES (à l'intérieur de la classe) =====

    public async Task MettreAJourAsync(Produit p)
    {
        await using var conn = _db.CreateProduitsConnection();
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            UPDATE Produits
            SET Nom = $nom,
                Categorie = $cat,
                PrixUnitaire = $prix,
                QuantiteStock = $qte,
                SeuilAlerte = $seuil
            WHERE Id = $id";
        cmd.Parameters.AddWithValue("$nom", p.Nom);
        cmd.Parameters.AddWithValue("$cat", p.Categorie);
        cmd.Parameters.AddWithValue("$prix", (double)p.PrixUnitaire);
        cmd.Parameters.AddWithValue("$qte", p.QuantiteStock);
        cmd.Parameters.AddWithValue("$seuil", p.SeuilAlerte);
        cmd.Parameters.AddWithValue("$id", p.Id);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task SupprimerAsync(int id)
    {
        await using var conn = _db.CreateProduitsConnection();
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        // Soft delete pour garder la traçabilité
        cmd.CommandText = "UPDATE Produits SET Actif = 0 WHERE Id = $id";
        cmd.Parameters.AddWithValue("$id", id);
        await cmd.ExecuteNonQueryAsync();
    }
}