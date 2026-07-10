using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Threading;
using Shouldly;
using Xunit;

namespace OpenSSH_GUI.Tests.ViewModels;

public class SmokeTests
{
    [AvaloniaFact]
    public async Task Clipboard_Roundtrip_Works_On_Headless_Window()
    {
        var window = new Window();
        window.Clipboard.ShouldNotBeNull();
        await window.Clipboard!.SetValueAsync(DataFormat.Text, "hello-world");
        await window.Clipboard!.FlushAsync();
        var text = await window.Clipboard!.TryGetTextAsync();
        text.ShouldBe("hello-world");
    }
}
