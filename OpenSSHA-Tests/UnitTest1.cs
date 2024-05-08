using OpenSSHALib.Lib.Structs;
using OpenSSHALib.Models;

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
        var ppKey = new PpkKey(@"C:\Users\frequ\.ssh\id_rsa_puttyKeygen.ppk");
        var openSsh = ppKey.ConvertToOpenSshKey(out var errorMessage);
        if(openSsh is null) Console.WriteLine(errorMessage);
        
    }
}