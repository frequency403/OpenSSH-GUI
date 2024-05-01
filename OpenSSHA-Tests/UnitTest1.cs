using System.Threading.Channels;
using OpenSSHALib.Lib;
using OpenSSHALib.Lib.Structs;

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
        var file = new PpkKey("C:\\Users\\frequ\\.ssh\\id_rsa_puttyKeygen.ppk");
        var key = file.ConvertToOpenSshKey();
        Console.WriteLine(file);
    }
}