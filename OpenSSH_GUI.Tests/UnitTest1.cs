#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 08.05.2024 - 22:05:30
// Last edit: 14.05.2024 - 03:05:19

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