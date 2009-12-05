﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using System.Net;
using BaconFTP.Data.Logger;

namespace BaconFTP.Server
{
    public static class FtpServer
    {
        #region Fields
        
        private static TcpListener _tcpListener;
        private static List<FtpClient> _connectedClients = new List<FtpClient>();
        
        //por ahora, despues hay que implementar uno para hacerlo en un archivo.
        private static ILogger _logger = new ConsoleLogger();
        
        #endregion //Fields

        #region Interface

        public static void Start()
        {
            Start(Const.DefaultFtpPort);
        }

        public static void Start(int port)
        {
            Start(IPAddress.Any, port);
        }

        public static void Start(IPAddress ipAddress, int port)
        {
            _tcpListener = new TcpListener(ipAddress, port);
            new Thread(ListenForConnections).Start();
        }

        public static void CloseConnection(FtpClient client)
        {
            _connectedClients.Remove(client);

            _logger.Write(String.Format("Connection with {0} closed.", client.EndPoint));

            client.CloseConnection();
        }

        #endregion //Interface

        #region Implementation

        private static void ListenForConnections()
        {
            _tcpListener.Start();

            while (true)
            {
                FtpClient ftpClient = new FtpClient(_tcpListener.AcceptTcpClient());

                _connectedClients.Add(ftpClient);

                new Thread(new ParameterizedThreadStart(HandleClientConnection)).Start(ftpClient);                
            }
        }

        private static void HandleClientConnection(object client)
        {
            FtpClient ftpClient = (FtpClient)client;

            _logger.Write(String.Format("Connection with {0} established.", ftpClient.EndPoint));

            FtpProtocol protocol = new FtpProtocol(ftpClient, _logger);

            if (protocol.PerformHandShake())
                protocol.ListenForCommands();
            else
                CloseConnection(ftpClient);
        }

        #endregion //implementation
    }
}