namespace OpenSSHALib.Enums;

public enum KeyType
{
    // ReSharper disable InconsistentNaming
    RSA = 4096,
    DSA = 3072,
    ECDSA = 521,
    EdDSA = ECDSA,
    Ed25519 = 255
}