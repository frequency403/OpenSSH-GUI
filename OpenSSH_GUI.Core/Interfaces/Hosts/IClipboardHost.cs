using Avalonia.Input.Platform;

namespace OpenSSH_GUI.Core.Interfaces.Hosts;

public interface IClipboardHost
{
    IClipboard? Clipboard { get; }
}