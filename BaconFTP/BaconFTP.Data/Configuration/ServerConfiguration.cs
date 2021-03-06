﻿using System;
using System.Linq;
using System.IO;
using System.Xml.Linq;
using BaconFTP.Data.Logger;
using BaconFTP.Data.Exceptions;

namespace BaconFTP.Data.Configuration
{
    public static class Const
    {
        internal static string ServerDirectoryElement = "directory";
        internal static string ServerConfigurationFilename = "server_config.xml";
        internal static string ServerDirectoryName = "baconftpd";
        internal static string DefaultPortElement = "port";        
        internal static string ConfigurationRootElement = "baconftp_config";
        internal static string LoggingMethodElement = "logging";
        internal static string ConsoleLoggingMethod = "console";
        internal static string FileLoggingMethod = "file";
        internal static string DefaultServerPort = "21";
    }

    //clase que maneja la configuracion del server en un archivo xml
    public static class ServerConfiguration
    {
        #region Fields

        private static string _pathToXmlFile = Const.ServerConfigurationFilename;
        private static XDocument _configXmlFile;
        private static XElement _root;

        #endregion

        #region Interface

        //parsea el archivo de configuracion para ver si es válido.
        public static void Parse()
        {
            try
            {
                _configXmlFile = XDocument.Load(GetConfigurationFile());
                _root = _configXmlFile.Root;

                if (_root.HasElements)
                {
                    if (_root.Element(Const.LoggingMethodElement) == null)
                        throw new LoggingElementNotFoundException();

                    else if (_root.Element(Const.DefaultPortElement) == null)
                        throw new ServerPortElementNotFoundException();

                    else if (_root.Element(Const.ServerDirectoryElement) == null)
                        throw new DirectoryElementNotFoundException();
                }
                else
                    throw new ConfigurationFileIsEmptyException();
            }

            catch (Exception e) { throw e; }
        }

        public static string ServerDirectoryPath
        {
            get { return GetValueFrom(Const.ServerDirectoryElement); }
            set { SetValue(Const.ServerDirectoryElement, value); }
        }

        public static int ServerPort
        {
            get { return Convert.ToInt32(GetValueFrom(Const.DefaultPortElement)); }
            set { SetValue(Const.DefaultPortElement, value.ToString()); }
        }

        public static string Logger
        {
            get { return GetValueFrom(Const.LoggingMethodElement); }
            set
            {
                if (string.Equals(Const.FileLoggingMethod, value, StringComparison.CurrentCultureIgnoreCase))
                    SetValue(Const.LoggingMethodElement, Const.FileLoggingMethod);

                else if (string.Equals(Const.ConsoleLoggingMethod, value, StringComparison.CurrentCultureIgnoreCase))
                    SetValue(Const.LoggingMethodElement, Const.ConsoleLoggingMethod);
            }
        }

        public static ILogger GetLoggerInstance()
        {
            string loggerTagValue = GetValueFrom(Const.LoggingMethodElement);

            if (loggerTagValue == Const.FileLoggingMethod)
                return new FileLogger();

            else if (loggerTagValue == Const.ConsoleLoggingMethod)
                return new ConsoleLogger();

            else
                throw new LoggingMethodUnknownException();
        }

        public static void GenerateConfigurationFile()
        {
            GenerateConfigurationFile(CreateDefaultServerFolder().FullName,
                                      Const.DefaultServerPort,
                                      Const.ConsoleLoggingMethod);
        }

        #endregion

        #region Implementation

        private static string GetConfigurationFile()
        {
            if (!File.Exists(_pathToXmlFile))
                GenerateConfigurationFile();

            return _pathToXmlFile;

        }

        private static string GetValueFrom(string element)
        {
            return (from d in _root.Elements()
                    where d.Name == element
                    select d).Single().Value;
        }

        private static void SetValue(string element, string value)
        {
            _root.Element(element).Value = value;
            _configXmlFile.Save(_pathToXmlFile);
        }

        private static void GenerateConfigurationFile(string directory, string port, string loggingMethod)
        {
            (new XDocument(
                new XDeclaration("1.0", "utf-8", "yes"),
                new XElement(Const.ConfigurationRootElement,
                             new XElement(Const.ServerDirectoryElement, directory),
                             new XElement(Const.DefaultPortElement, port),
                             new XElement(Const.LoggingMethodElement, loggingMethod)
                             )
                           )
            ).Save(_pathToXmlFile);
        }

        private static DirectoryInfo CreateDefaultServerFolder()
        {
            return Directory.CreateDirectory(Const.ServerDirectoryName);
        }

        #endregion
    }
}
