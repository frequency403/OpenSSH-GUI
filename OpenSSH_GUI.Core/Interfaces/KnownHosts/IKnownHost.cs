#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 15.05.2024 - 00:05:44
// Last edit: 15.05.2024 - 01:05:34

#endregion

using ReactiveUI;

namespace OpenSSH_GUI.Core.Interfaces.KnownHosts;

/// <summary>
///     Represents a known host in the OpenSSH GUI.
/// </summary>
public interface IKnownHost : IReactiveObject
{
    /// <summary>
    ///     Represents a known host.
    /// </summary>
    string Host { get; }

    /// <summary>
    ///     Represents a known host in the OpenSSH GUI.
    /// </summary>
    bool DeleteWholeHost { get; }

    /// <summary>
    ///     Represents a known host in the OpenSSH GUI.
    /// </summary>
    List<IKnownHostKey> Keys { get; set; }

    /// <summary>
    ///     Toggles the marked for deletion flag of each <see cref="IKnownHostKey" /> within the <see cref="Keys" /> list.
    ///     If the <see cref="SwitchToggled" /> property is true, it sets the flag to false for all keys. Otherwise, it sets
    ///     the flag to true for all keys.
    /// </summary>
    void KeysDeletionSwitch();

    /// <summary>
    ///     Retrieves all entries for a known host in the known hosts file.
    /// </summary>
    /// <returns>
    ///     Returns a string containing all the entries for the known host.
    ///     If the entire host is marked for deletion, returns the line ending character.
    /// </returns>
    string GetAllEntries();
}