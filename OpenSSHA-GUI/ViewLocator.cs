#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 20.12.2023 - 23:12:01
// Last edit: 08.05.2024 - 22:05:56

#endregion

using System;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using OpenSSHA_GUI.ViewModels;

namespace OpenSSHA_GUI;

public class ViewLocator : IDataTemplate
{
    public Control? Build(object? data)
    {
        if (data is null)
            return null;

        var name = data.GetType().FullName!.Replace("ViewModel", "View", StringComparison.Ordinal);
        var type = Type.GetType(name);

        if (type != null)
        {
            var control = (Control)Activator.CreateInstance(type)!;
            control.DataContext = data;
            return control;
        }

        return new TextBlock { Text = "Not Found: " + name };
    }

    public bool Match(object? data)
    {
        return data is ViewModelBase;
    }
}