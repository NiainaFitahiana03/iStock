namespace IStock.Models;

public class Produit
{
    public int Id { get; set; }
    public string Nom { get; set; } = "";
    public string Categorie { get; set; } = "";
    public decimal PrixUnitaire { get; set; }
    public int QuantiteStock { get; set; }
    public int SeuilAlerte { get; set; } = 5;
    public bool Actif { get; set; } = true;
}