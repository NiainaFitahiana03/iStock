using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel;
using IStock.Models;
using Microsoft.Data.Sqlite;

namespace IStock.Services;

public class ImportService
{
    private readonly ProduitService _produitService;

    public ImportService(ProduitService produitService)
    {
        _produitService = produitService;
    }

    // ===== Point d'entrée =====
    public async Task<ImportResultat> ImporterAsync(string filePath)
    {
        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        return ext switch
        {
            ".xlsx" => await ImporterExcelAsync(filePath),
            ".db" or ".sqlite" or ".sqlite3" => await ImporterSqliteAsync(filePath),

            // Formats non supportés mais avec un message explicite
            ".ods" => new ImportResultat
            {
                ErreurGlobale = "Format LibreOffice (.ods) non supporté directement. " +
                                "Veuillez l'enregistrer au format .xlsx (Excel) avant l'import : " +
                                "Fichier → Enregistrer sous → Classeur Excel 2007-365 (.xlsx)."
            },
            ".numbers" => new ImportResultat
            {
                ErreurGlobale = "Format Apple Numbers (.numbers) non supporté. " +
                                "Veuillez l'exporter au format .xlsx : " +
                                "Fichier → Exporter vers → Excel."
            },
            ".xls" => new ImportResultat
            {
                ErreurGlobale = "Ancien format Excel (.xls) non supporté. " +
                                "Veuillez l'enregistrer au format .xlsx."
            },
            ".csv" => new ImportResultat
            {
                ErreurGlobale = "Format CSV non supporté. " +
                                "Veuillez l'ouvrir dans un tableur et l'enregistrer au format .xlsx."
            },
            _ => new ImportResultat
            {
                ErreurGlobale = $"Format « {ext} » non supporté. Utilisez .xlsx ou .db."
            }
        };
    }

    // ===== Excel (.xlsx) =====
    private async Task<ImportResultat> ImporterExcelAsync(string path)
    {
        var result = new ImportResultat();

        IXLWorkbook wb;
        try
        {
            wb = new XLWorkbook(path);
        }
        catch (Exception ex)
        {
            result.ErreurGlobale = $"Impossible d'ouvrir le fichier Excel : {ex.Message}";
            return result;
        }

        using (wb)
        {
            var ws = wb.Worksheets.FirstOrDefault();
            if (ws is null)
            {
                result.ErreurGlobale = "Le fichier ne contient aucune feuille.";
                return result;
            }

            // Lire l'entête (ligne 1)
            var headers = ws.Row(1).CellsUsed()
                .Select(c => c.GetString().Trim().ToLowerInvariant())
                .ToList();

            var colonnesRequises = new[] { "nom", "categorie", "prixunitaire", "quantitestock", "seuilalerte" };

            var colonnesManquantes = colonnesRequises
                .Where(c => !headers.Contains(c))
                .ToList();

            if (colonnesManquantes.Count > 0)
            {
                result.ErreurGlobale =
                    $"Colonnes manquantes : {string.Join(", ", colonnesManquantes)}. " +
                    $"Colonnes attendues (ligne 1) : {string.Join(", ", colonnesRequises)}.";
                return result;
            }

            int Idx(string nom) => headers.IndexOf(nom);
            int iNom = Idx("nom");
            int iCat = Idx("categorie");
            int iPrix = Idx("prixunitaire");
            int iQte = Idx("quantitestock");
            int iSeuil = Idx("seuilalerte");

            var rows = ws.RowsUsed().Skip(1).ToList();
            result.TotalLignes = rows.Count;

            foreach (var row in rows)
            {
                var numero = row.RowNumber();
                try
                {
                    // Ignorer les lignes complètement vides
                    var nomCell = row.Cell(iNom + 1).GetString().Trim();
                    if (string.IsNullOrWhiteSpace(nomCell) &&
                        string.IsNullOrWhiteSpace(row.Cell(iCat + 1).GetString()) &&
                        string.IsNullOrWhiteSpace(row.Cell(iPrix + 1).GetString()))
                    {
                        result.TotalLignes--;
                        continue;
                    }

                    var categorie = row.Cell(iCat + 1).GetString().Trim();
                    var prixCell = row.Cell(iPrix + 1);
                    var qteCell = row.Cell(iQte + 1);
                    var seuilCell = row.Cell(iSeuil + 1);

                    // Validations
                    if (string.IsNullOrWhiteSpace(nomCell))
                        throw new Exception("Nom vide.");
                    if (string.IsNullOrWhiteSpace(categorie))
                        throw new Exception("Catégorie vide.");
                    if (!prixCell.TryGetValue<decimal>(out var prix) || prix < 0)
                        throw new Exception("Prix invalide (doit être un nombre ≥ 0).");
                    if (!qteCell.TryGetValue<int>(out var qte) || qte < 0)
                        throw new Exception("Quantité invalide (doit être un entier ≥ 0).");

                    int seuil = 5;
                    var seuilStr = seuilCell.GetString().Trim();
                    if (!string.IsNullOrWhiteSpace(seuilStr))
                    {
                        if (!seuilCell.TryGetValue<int>(out seuil) || seuil < 0)
                            throw new Exception("Seuil invalide (doit être un entier ≥ 0).");
                    }

                    await _produitService.AjouterAsync(new Produit
                    {
                        Nom = nomCell,
                        Categorie = categorie,
                        PrixUnitaire = prix,
                        QuantiteStock = qte,
                        SeuilAlerte = seuil
                    });
                    result.LignesImportees++;
                }
                catch (Exception ex)
                {
                    result.Erreurs.Add($"Ligne {numero} : {ex.Message}");
                }
            }
        }

        return result;
    }

    // ===== SQLite (.db) =====
    private async Task<ImportResultat> ImporterSqliteAsync(string path)
    {
        var result = new ImportResultat();

        SqliteConnection conn;
        try
        {
            conn = new SqliteConnection($"Data Source={path}");
            await conn.OpenAsync();
        }
        catch (Exception ex)
        {
            result.ErreurGlobale = $"Impossible d'ouvrir la base SQLite : {ex.Message}";
            return result;
        }

        await using (conn)
        {
            // Vérifier que la table Produits existe
            using (var checkCmd = conn.CreateCommand())
            {
                checkCmd.CommandText =
                    "SELECT name FROM sqlite_master WHERE type='table' AND name='Produits'";
                var tableExists = await checkCmd.ExecuteScalarAsync() is not null;
                if (!tableExists)
                {
                    result.ErreurGlobale =
                        "Le fichier .db ne contient pas de table 'Produits'. " +
                        "La table doit contenir au minimum : Nom, Categorie, PrixUnitaire, QuantiteStock, SeuilAlerte.";
                    return result;
                }
            }

            // Vérifier les colonnes
            var colonnesRequises = new[] { "Nom", "Categorie", "PrixUnitaire", "QuantiteStock", "SeuilAlerte" };
            var colonnesDisponibles = new List<string>();

            using (var infoCmd = conn.CreateCommand())
            {
                infoCmd.CommandText = "PRAGMA table_info(Produits)";
                await using var reader = await infoCmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                    colonnesDisponibles.Add(reader.GetString(1));
            }

            var manquantes = colonnesRequises
                .Where(c => !colonnesDisponibles.Contains(c, StringComparer.OrdinalIgnoreCase))
                .ToList();

            if (manquantes.Count > 0)
            {
                result.ErreurGlobale =
                    $"Colonnes manquantes dans la table Produits : {string.Join(", ", manquantes)}.";
                return result;
            }

            // Lire et importer
            using var selCmd = conn.CreateCommand();
            selCmd.CommandText =
                "SELECT Nom, Categorie, PrixUnitaire, QuantiteStock, SeuilAlerte FROM Produits";
            await using var rdr = await selCmd.ExecuteReaderAsync();

            var ligne = 1;
            while (await rdr.ReadAsync())
            {
                ligne++;
                try
                {
                    var nom = rdr.GetString(0).Trim();
                    var categorie = rdr.GetString(1).Trim();
                    var prix = (decimal)rdr.GetDouble(2);
                    var qte = rdr.GetInt32(3);
                    var seuil = rdr.GetInt32(4);

                    if (string.IsNullOrWhiteSpace(nom))
                        throw new Exception("Nom vide.");
                    if (string.IsNullOrWhiteSpace(categorie))
                        throw new Exception("Catégorie vide.");
                    if (prix < 0) throw new Exception("Prix négatif.");
                    if (qte < 0) throw new Exception("Quantité négative.");
                    if (seuil < 0) throw new Exception("Seuil négatif.");

                    result.TotalLignes++;
                    await _produitService.AjouterAsync(new Produit
                    {
                        Nom = nom,
                        Categorie = categorie,
                        PrixUnitaire = prix,
                        QuantiteStock = qte,
                        SeuilAlerte = seuil
                    });
                    result.LignesImportees++;
                }
                catch (Exception ex)
                {
                    result.Erreurs.Add($"Ligne {ligne} : {ex.Message}");
                }
            }
        }

        return result;
    }
}

// ===== Résultat d'import =====
public class ImportResultat
{
    public int TotalLignes { get; set; }
    public int LignesImportees { get; set; }
    public List<string> Erreurs { get; } = new();
    public string? ErreurGlobale { get; set; }
    public bool Succes => ErreurGlobale is null;
}