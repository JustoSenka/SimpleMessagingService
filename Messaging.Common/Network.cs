using System;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace Messaging.Common
{
    public static class Network
    {
        public const string IpRegex = @"^\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}$";

        public static string GetLocalIP()
        {
            string localIP;
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
            {
                socket.Connect("8.8.8.8", 65530);
                IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
                localIP = endPoint.Address.ToString();
            }
            return localIP.Trim();
        }

        public static string GetExternalIPFromCombinedSources()
        {
            var ip1 = GetExternalIP_1();
            if (Regex.IsMatch(ip1, IpRegex))
                return ip1;

            var ip2 = GetExternalIP_2();
            if (Regex.IsMatch(ip2, IpRegex))
                return ip2;

            var ip3 = GetExternalIP_3();
            if (Regex.IsMatch(ip3, IpRegex))
                return ip3;

            throw new Exception("IPs from all different sources did not match the correct IP format");
        }

        public static string GetExternalIP_1()
        {
            return new WebClient().DownloadString("http://icanhazip.com").Trim();
        }

        public static string GetExternalIP_2()
        {
            return new WebClient().DownloadString("https://api.ipify.org").Trim();
        }

        public static string GetExternalIP_3()
        {
            return new WebClient().DownloadString("http://ifconfig.me").Trim();
        }
    }
}
