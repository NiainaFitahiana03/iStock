using System;
using IStock.ViewModels;

namespace IStock.Models;

public record MenuItem(string Title, Func<ViewModelBase> CreatePage);