#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 15.05.2024 - 00:05:44
// Last edit: 15.05.2024 - 01:05:35

#endregion

using System.Security.Cryptography;
using System.Text;
using OpenSSH_GUI.Core.Extensions;
using OpenSSH_GUI.Core.Lib.Credentials;

namespace OpenSSHA_Tests;

public class Tests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public async Task Test1()
    {
        var content = await File.ReadAllTextAsync(@"C:\Users\frequ\AppData\Roaming\OpenSSH_GUI\OpenSSH_GUI.json");
        var encrypted = content.Encrypt();

        
        Assert.That(string.Equals(content, encrypted.Decrypt()));
    }
}