using System.Diagnostics;
using OpenSSHALib.Enums;

namespace OpenSSHALib.Models;

public class SshPrivateKey(string absoluteFilePath) : SshKey(absoluteFilePath);