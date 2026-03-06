using Avalonia.Input.Platform;

namespace OpenSSH_GUI.Core.Interfaces;

public interface IClipboardHost
{
    IClipboard? Clipboard { get; }
}