using Avalonia.Controls;
using Avalonia.Controls.Templates;
using System;
using IStock.ViewModels;

namespace IStock;

public class ViewLocator : IDataTemplate
{
    public Control? Build(object? data)
    {
        if (data is null) return null;

        var vmType = data.GetType();
        var viewName = vmType.FullName!
            .Replace(".ViewModels.Pages.", ".Views.")
            .Replace(".ViewModels.", ".Views.")
            .Replace("ViewModel", "View");

        var type = Type.GetType(viewName);
        if (type is null)
            return new TextBlock { Text = "Not Found: " + viewName };

        return (Control)Activator.CreateInstance(type)!;
    }

    public bool Match(object? data) => data is ViewModelBase;
}