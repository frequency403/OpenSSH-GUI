#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 08.05.2024 - 22:05:30
// Last edit: 08.05.2024 - 22:05:01

#endregion

using OpenSSHALib.Enums;
using OpenSSHALib.Interfaces;
using ReactiveUI;

namespace OpenSSHALib.Models;

public class KnownHostKey : ReactiveObject, IKnownHostKey
{
    private bool _markedForDeletion;

    public KnownHostKey(string entry)
    {
        EntryWithoutHost = entry;
        var splitted = EntryWithoutHost.Split(' ');
        TypeDeclarationInFile = splitted[0];
        KeyType = Enum.Parse<KeyType>(
            TypeDeclarationInFile.StartsWith("ssh-")
                ? TypeDeclarationInFile.Replace("ssh-", "")
                : TypeDeclarationInFile.Split('-')[0], true);
        Fingerprint = splitted[1].Replace("\n", "").Replace("\r", "");
    }

    private string TypeDeclarationInFile { get; }

    public KeyType KeyType { get; }
    public string Fingerprint { get; }
    public string EntryWithoutHost { get; }

    public bool MarkedForDeletion
    {
        get => _markedForDeletion;
        set => this.RaiseAndSetIfChanged(ref _markedForDeletion, value);
    }
}