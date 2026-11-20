using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using IStock.Data;
using IStock.Models;

namespace IStock.Services;

public class HistoriqueService
{
    private readonly DatabaseService _db;
    public HistoriqueService(DatabaseService db) => _db = db;

    public async Task<List<MouvementStock>> GetMouvementsAsync()
    {
        var list = new List<MouvementStock>();
        await using var conn = _db.CreateHistoriqueConnection();
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"SELECT Id, ProduitId, Type, Quantite, DateMouvement, Raison
                            FROM MouvementsStock
                            ORDER BY DateMouvement DESC";
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(new MouvementStock
            {
                Id = reader.GetInt32(0),
                ProduitId = reader.GetInt32(1),
                Type = reader.GetString(2),
                Quantite = reader.GetInt32(3),
                DateMouvement = DateTime.Parse(reader.GetString(4)),
                Raison = reader.IsDBNull(5) ? null : reader.GetString(5)
            });
        }
        return list;
    }
}