using System;

namespace IStock.Models;

public class Notification
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Titre { get; set; } = "";
    public string Message { get; set; } = "";
    public NotificationNiveau Niveau { get; set; } = NotificationNiveau.Info;
    public DateTime DateCreation { get; set; } = DateTime.Now;
    public bool EstLue { get; set; }
}

public enum NotificationNiveau
{
    Info,
    Avertissement,
    Critique
}