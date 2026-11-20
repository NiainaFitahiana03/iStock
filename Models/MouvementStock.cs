using System;

namespace IStock.Models;

public class MouvementStock
{
    public int Id { get; set; }
    public int ProduitId { get; set; }
    public string Type { get; set; } = "";   // 'entree', 'sortie', 'ajustement'
    public int Quantite { get; set; }
    public DateTime DateMouvement { get; set; }
    public string? Raison { get; set; }
}