using System;
using System.Collections.Generic;

namespace IStock.Models;

public class Vente
{
    public int Id { get; set; }
    public DateTime DateVente { get; set; }
    public decimal Total { get; set; }
    public string Statut { get; set; } = "validee";
    public List<VenteDetail> Details { get; set; } = new();
}

public class VenteDetail
{
    public int Id { get; set; }
    public int VenteId { get; set; }
    public int ProduitId { get; set; }
    public string NomProduit { get; set; } = "";
    public decimal PrixUnitaire { get; set; }
    public int Quantite { get; set; }
}