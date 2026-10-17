#region "copyright"

/*
    Copyright © 2025 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using NINA.Core.Utility;

namespace ninaAPI.Utility
{
    public static class LocalAddresses
    {
        public static string LocalHostName { get; }
        public static string IPAddress { get; }
        public static string HostName { get; }

        static LocalAddresses()
        {
            LocalHostName = "localhost";
            HostName = Dns.GetHostName();
            IPAddress = NetworkUtility.GetIPv4Address();
        }
    }

    public static class NetworkUtility
    {
        public static bool IsPortAvailable(int port)
        {
            bool isPortAvailable = true;

            IPGlobalProperties ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
            IPEndPoint[] ipEndPoints = ipGlobalProperties.GetActiveTcpListeners();

            foreach (IPEndPoint endPoint in ipEndPoints)
            {
                if (endPoint.Port == port)
                {
                    isPortAvailable = false;
                    break;
                }
            }

            return isPortAvailable;
        }

        public static int GetNearestAvailablePort(int startPort)
        {
            using var watch = MyStopWatch.Measure();
            int port = startPort;
            while (!IsPortAvailable(port))
            {
                port++;
            }
            return port;
        }

        public static string GetIPv4Address()
        {
            string localIP;
            try
            {
                using Socket socket = new(AddressFamily.InterNetwork, SocketType.Dgram, 0);
                socket.Connect("8.8.8.8", 65530);
                IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
                localIP = endPoint.Address.ToString();
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to get local IP address: {ex}");
                localIP = "127.0.0.1";
            }

            return localIP;
        }
    }
}