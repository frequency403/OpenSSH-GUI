using System.Threading.Channels;
using OpenSSHALib.Lib;

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
        PpkToOpenSsh.ConvertFile("C:\\Users\\frequ\\.ssh\\id_rsa_puttyKeygen.ppk").Select(e =>
        {
            Console.WriteLine(e);
            return e;
        });
    }
}