#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 15.05.2024 - 00:05:44
// Last edit: 15.05.2024 - 01:05:30

#endregion

using OpenSSH_GUI.Core.Enums;
using OpenSSH_GUI.Core.Interfaces.KnownHosts;
using ReactiveUI;

namespace OpenSSH_GUI.Core.Lib.KnownHosts;

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