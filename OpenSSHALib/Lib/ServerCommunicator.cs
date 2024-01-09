using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using Renci.SshNet;

namespace OpenSSHALib.Lib;

public static class ServerCommunicator
{
    public static bool TestConnection(string ipAdressOrHostname, [NotNullWhen(false)]out string? message)
    {
        message = null;
        if (!NetworkInterface.GetIsNetworkAvailable())
        {
            message = "Machine is not connected to the Internet";
            return false;
        }

        using var ping = new Ping();
        return ping.Send(ipAdressOrHostname).Status == IPStatus.Success;
    }

    public static bool PingSshConnection(string ipAdressOrHostname, string user, string userPassword,[NotNullWhen(false)] out string? message)
    {
        message = null;
        try
        {
            using var sshClient = new SshClient(ipAdressOrHostname, user, userPassword);
            sshClient.Connect();
            var connectionResult = sshClient.IsConnected;
            sshClient.Disconnect();
            return connectionResult;
        }
        catch (Exception e)
        {
            message = e.Message;
            return false;
        }
    }
}