using Microsoft.Data.Sqlite;
using System;
using System.IO;

namespace IStock.Data;

public class DatabaseService
{
    private readonly string _produitsPath;
    private readonly string _historiquePath;

    public DatabaseService()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var folder = Path.Combine(appData, "IStock");
        Directory.CreateDirectory(folder);

        _produitsPath = Path.Combine(folder, "produits.db");
        _historiquePath = Path.Combine(folder, "historique.db");

        InitializeProduits();
        InitializeHistorique();
    }

    public SqliteConnection CreateProduitsConnection() =>
        new($"Data Source={_produitsPath}");

    public SqliteConnection CreateHistoriqueConnection() =>
        new($"Data Source={_historiquePath}");

    private void InitializeProduits()
    {
        using var conn = CreateProduitsConnection();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS Produits (
                Id            INTEGER PRIMARY KEY AUTOINCREMENT,
                Nom           TEXT NOT NULL,
                Categorie     TEXT NOT NULL,
                PrixUnitaire  REAL NOT NULL CHECK(PrixUnitaire >= 0),
                QuantiteStock INTEGER NOT NULL DEFAULT 0 CHECK(QuantiteStock >= 0),
                SeuilAlerte   INTEGER NOT NULL DEFAULT 5,
                Actif         INTEGER NOT NULL DEFAULT 1
            );
            CREATE INDEX IF NOT EXISTS idx_produits_nom ON Produits(Nom);
            CREATE INDEX IF NOT EXISTS idx_produits_categorie ON Produits(Categorie);
        ";
        cmd.ExecuteNonQuery();
    }

    private void InitializeHistorique()
    {
        using var conn = CreateHistoriqueConnection();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS Ventes (
                Id        INTEGER PRIMARY KEY AUTOINCREMENT,
                DateVente TEXT NOT NULL,
                Total     REAL NOT NULL,
                Statut    TEXT NOT NULL DEFAULT 'validee'
            );
            CREATE TABLE IF NOT EXISTS VenteDetails (
                Id           INTEGER PRIMARY KEY AUTOINCREMENT,
                VenteId      INTEGER NOT NULL,
                ProduitId    INTEGER NOT NULL,
                NomProduit   TEXT NOT NULL,
                PrixUnitaire REAL NOT NULL,
                Quantite     INTEGER NOT NULL,
                FOREIGN KEY (VenteId) REFERENCES Ventes(Id)
            );
            CREATE TABLE IF NOT EXISTS MouvementsStock (
                Id            INTEGER PRIMARY KEY AUTOINCREMENT,
                ProduitId     INTEGER NOT NULL,
                Type          TEXT NOT NULL,
                Quantite      INTEGER NOT NULL,
                DateMouvement TEXT NOT NULL,
                Raison        TEXT
            );
            CREATE INDEX IF NOT EXISTS idx_ventes_date ON Ventes(DateVente);
            CREATE INDEX IF NOT EXISTS idx_details_vente ON VenteDetails(VenteId);
            CREATE INDEX IF NOT EXISTS idx_details_produit ON VenteDetails(ProduitId);
            CREATE INDEX IF NOT EXISTS idx_mouvements_produit ON MouvementsStock(ProduitId);
            CREATE INDEX IF NOT EXISTS idx_mouvements_date ON MouvementsStock(DateMouvement);
        ";
        cmd.ExecuteNonQuery();
    }
}