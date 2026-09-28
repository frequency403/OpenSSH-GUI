using Avalonia.Threading;
using Material.Icons;
using OpenSSH_GUI.Core.Interfaces;
using OpenSSH_GUI.Core.Lib.HostKeys;
using OpenSSH_GUI.Dialogs.Enums;
using OpenSSH_GUI.Dialogs.Interfaces;
using OpenSSH_GUI.Resources;

namespace OpenSSH_GUI.Services;

/// <summary>
///     Shows the fingerprint of an unknown server host key and lets the user decide whether to trust it.
/// </summary>
public sealed class HostKeyTrustPrompt(IMessageBoxProvider messageBoxProvider) : IHostKeyTrustPrompt
{
    /// <inheritdoc />
    public async Task<bool> ConfirmUnknownHostKeyAsync(HostKeyInfo hostKey)
    {
        var result = await Dispatcher.UIThread.InvokeAsync(() => messageBoxProvider.ShowMessageBoxAsync(
            StringsAndTexts.HostKeyUnknownTitle,
            string.Format(
                StringsAndTexts.HostKeyUnknownText, hostKey.KnownHostsName, hostKey.KeyType,
                hostKey.FingerprintSha256),
            MessageBoxButtons.YesNo,
            MaterialIconKind.ShieldAlertOutline));
        return result is MessageBoxResult.Yes;
    }
}
