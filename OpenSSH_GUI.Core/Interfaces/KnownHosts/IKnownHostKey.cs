#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 15.05.2024 - 00:05:44
// Last edit: 15.05.2024 - 01:05:34

#endregion

using OpenSSH_GUI.Core.Enums;
using ReactiveUI;

namespace OpenSSH_GUI.Core.Interfaces.KnownHosts;

/// Represents a known host key in the OpenSSH GUI.
/// /
public interface IKnownHostKey : IReactiveObject
{
    /// <summary>
    ///     Represents the type of a known host key.
    /// </summary>
    KeyType KeyType { get; }

    /// <summary>
    ///     Represents a known host key in the OpenSSH GUI.
    /// </summary>
    string Fingerprint { get; }

    /// <summary>
    ///     Represents a known host key in the OpenSSH GUI.
    /// </summary>
    string EntryWithoutHost { get; }

    /// <summary>
    ///     Gets or sets whether the KnownHostKey is marked for deletion.
    /// </summary>
    bool MarkedForDeletion { get; set; }
}