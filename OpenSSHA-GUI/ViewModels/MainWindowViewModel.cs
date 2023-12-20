using System;
using System.Windows.Input;
using ReactiveUI;

namespace OpenSSHA_GUI.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private string[] _wordCollection = ["Welcome", "to", "Avalonia"];

    public MainWindowViewModel()
    {
        Greeting = "Welcome to Avalonia!";
    }
    
#pragma warning disable CA1822 // Mark members as static
    private string _greeting;
    public string Greeting
    {
        get => _greeting;
        set => this.RaiseAndSetIfChanged(ref _greeting, value);
    }  
#pragma warning restore CA1822 // Mark members as static

    public void Shuffle()
    {
        var random = new Random();
        random.Shuffle(_wordCollection);
        Greeting = $"{_wordCollection[0]} {_wordCollection[1]} {_wordCollection[2]}";
        Console.WriteLine($"Greeting is: {Greeting}");

        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    }
}