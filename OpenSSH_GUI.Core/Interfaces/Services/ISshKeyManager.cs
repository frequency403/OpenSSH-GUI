using OpenSSH_GUI.Core.Lib.Keys;
using ReactiveUI;
using SshNet.Keygen;

namespace OpenSSH_GUI.Core.Interfaces.Services;

/// <summary>
///     Manager for SSH keys on the local machine.
///     Provides functionality for searching, generating, and changing formats of SSH keys.
/// </summary>
public interface ISshKeyManager : IReactiveNotifyPropertyChanged<IReactiveObject>, IHandleObservableErrors,
    IReactiveObject
{
    /// <summary>
    ///     Gets the collection of detected SSH keys.
    /// </summary>
    IReadOnlyCollection<SshKeyFile> SshKeys { get; }

    /// <summary>
    ///     Changes the format of an existing SSH key.
    /// </summary>
    /// <param name="key">The SSH key file to change.</param>
    /// <param name="newFormat">The target SSH key format.</param>
    /// <param name="token">A cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ChangeFormatOfKeyAsync(
        SshKeyFile key,
        SshKeyFormat newFormat,
        CancellationToken token = default);

    /// <summary>
    ///     Changes the order of the SSH keys in the collection.
    /// </summary>
    /// <param name="orderFunc">Function to reorder the keys.</param>
    void ChangeOrder(Func<IEnumerable<SshKeyFile>, IEnumerable<SshKeyFile>> orderFunc);

    /// <summary>
    ///     Generates a new SSH key.
    /// </summary>
    /// <param name="fullFilePath">The full path where the new key should be stored.</param>
    /// <param name="generateParamsInfo">Parameters for key generation.</param>
    /// <returns>A value task representing the asynchronous operation.</returns>
    ValueTask GenerateNewKey(string fullFilePath, SshKeyGenerateInfo generateParamsInfo);

    /// <summary>
    ///     Triggers a re-search for SSH keys on the disk.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task RerunSearchAsync();
}