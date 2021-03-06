﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using BaconFTP.Data.Configuration;
using BaconFTP.Data.Logger;

namespace BaconFTP.Server
{
    public static class FtpServer
    {
        #region Fields
        
        private static TcpListener _tcpListener;
        private static Thread _listenerThread = new Thread(ListenForConnections);
        private static List<FtpClient> _connectedClients = new List<FtpClient>();        
        
        private static ILogger _logger;
        
        #endregion //Fields

        #region Properties

        public static IPAddress ServerIP
        {
            get { return IPAddress.Parse(new WebClient().DownloadString("http://whatismyip.org")); }
        }

        public static int ServerPort
        {
            get { return ServerConfiguration.ServerPort; }
        }

        #endregion

        #region Constructor(s)

        static FtpServer()
        {
            try 
            { 
                ServerConfiguration.Parse();
            }
            catch (Exception e) { throw e; }
        }

        #endregion

        #region Interface

        public static void Start()
        {
            Start(ServerConfiguration.ServerPort);
        }

        public static void Start(int port)
        {
            Start(IPAddress.Any, port);
        }

        public static void Start(IPAddress ipAddress)
        {
            Start(ipAddress, ServerConfiguration.ServerPort);
        }

        public static void Start(IPAddress ipAddress, int port)
        {
            _tcpListener = new TcpListener(ipAddress, port);
            _logger = ServerConfiguration.GetLoggerInstance();

            _listenerThread.Start();
        }

        //cierra la conexion con un cliente
        internal static void CloseConnection(FtpClient client)
        {
            _connectedClients.Remove(client);

            _logger.Write("Connection with {0} closed.", client.EndPoint);

            client.CloseConnection();
        }

        //devuelve el path completo de un directorio virtual del server (ej "/" = c:\franklin\test")
        public static string GetRealPath(string virtualPath)
        {
            return ((virtualPath.Length == 1) && (virtualPath == "/")) ? 
                ServerConfiguration.ServerDirectoryPath + "\\" :
                ServerConfiguration.ServerDirectoryPath + virtualPath.Replace('/', '\\');
        }

        #endregion //Interface

        #region Implementation

        //MODIFICADO

        //se mantiene escuchando por conexiones, le asigna un thread a cada cliente que se conecte.
        private static void ListenForConnections()
        {
            try
            {
                _tcpListener.Start();

                _logger.Write("BaconFTP Server v{0} started successfully.\nListening on port {1}",
                              Const.ServerVersion, _tcpListener.LocalEndpoint.ToString().Split(':').Skip(1).First());

                while (true)
                {
                    FtpClient ftpClient = new FtpClient(_tcpListener.AcceptTcpClient());

                    _connectedClients.Add(ftpClient);

                    new Thread(new ParameterizedThreadStart(HandleClientConnection)).Start(ftpClient);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(Const.FatalErrorFormatString, e.Message);                
                Console.ReadKey();
            }
        }

        //maneja la conexion del cliente
        private static void HandleClientConnection(object client)
        {
            FtpClient ftpClient = (FtpClient)client;

            _logger.Write("Connection with {0} established.", ftpClient.EndPoint);            

            var protocolInterpreter = new FtpProtocolInterpreter(ftpClient, _logger);

            if (protocolInterpreter.PerformHandShake())
                protocolInterpreter.ListenForCommands();
            else
                CloseConnection(ftpClient);
        }

        #endregion //implementation
    }
}
