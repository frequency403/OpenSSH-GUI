using System;
using System.Reactive;
using Avalonia.Controls;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ReactiveUI;

namespace OpenSSHA_GUI.ViewModels;

public class ConfirmDialogViewModel
{
    public ConfirmDialogViewModel() : this("Allright?", "Yes", "No"){}

    public ConfirmDialogViewModel(string question, string okText, string notOkText)
    {
        DialogResult = ReactiveCommand.Create<string, ConfirmDialogViewModel?>(
            d => { Consent = bool.Parse(d);
                return this;
            });
        Question = question;
        OkText = okText;
        NotOkText = notOkText;
    }
    
    public string Question { get; }
    public string OkText { get; }
    public string NotOkText { get; }
    
    public bool Consent { get; set; } = false;
    public ReactiveCommand<string, ConfirmDialogViewModel> DialogResult { get; } 
}