#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 15.05.2024 - 00:05:44
// Last edit: 15.05.2024 - 01:05:35

#endregion

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
    public void Test1()
    {
        var cc = new PasswordConnectionCredentials("123", "123", "thisisaPassword");
        var ccc = cc;
        cc.EncryptPassword();
        cc.DecryptPassword();
        Assert.That(ccc.Password == cc.Password);
    }
}