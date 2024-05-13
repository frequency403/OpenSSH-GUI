// File Created by: Oliver Schantz
// Created: 13.05.2024 - 15:05:16
// Last edit: 13.05.2024 - 15:05:16

namespace OpenSSHALib.Interfaces;

public interface IMultiKeyConnectionCredentials : IConnectionCredentials
{
    IEnumerable<ISshKey> Keys { get; init; }
}