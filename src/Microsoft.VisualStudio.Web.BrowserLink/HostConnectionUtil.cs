// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Text;
using System.Threading;

namespace Microsoft.VisualStudio.Web.BrowserLink
{
    internal static class HostConnectionUtil
    {
        // The maximum amount of time we will wait for the host server to start.
        // The actual time it takes should be significantly less than this.
        private static readonly TimeSpan HostStartupTimeout = TimeSpan.FromMilliseconds(1500);

        // These were well-known values in V1 instance files. V2 instance files
        // allow these to be overridden.
        private static readonly IReadOnlyDictionary<string, string> V1DefaultProperties = new Dictionary<string, string>()
        {
            { BrowserLinkConstants.HostNameKey, "localhost" },
            { BrowserLinkConstants.FetchScriptVerbKey, "browserLink" },
            { BrowserLinkConstants.InjectScriptVerbKey, "injectScriptLink" },
            { BrowserLinkConstants.MappingDataVerbKey, "sendMappingData" }
        };

        /// <summary>
        /// Find the host connection for an ASP.NET application.
        /// </summary>
        /// <param name="applicationPhysicalPath">The physical root path of the application</param>
        /// <param name="connection">The connection that is found.</param>
        /// <returns>True if a connection is found for the given application.</returns>
        internal static bool FindHostConnection(string applicationPhysicalPath, out HostConnectionData connection)
        {
            applicationPhysicalPath = PathUtil.NormalizeDirectoryPath(applicationPhysicalPath);

            foreach (string instanceFileName in GetAllInstanceFileNames())
            {
                foreach (HostConnectionData connectionCandidate in ReadConnectionData(instanceFileName))
                {
                    if (ConnectionContainsApplication(connectionCandidate, applicationPhysicalPath))
                    {
                        connection = connectionCandidate;
                        return true;
                    }
                }
            }

            connection = null;
            return false;
        }

        /// <summary>
        /// Send a signal to the host to make sure the it is running.
        /// </summary>
        /// <param name="applicationPhysicalPath">The root path of this application</param>
        /// <param name="blockUntilStarted">If true, this method will not return until the host signals that it is ready.</param>
        /// <returns>True if the handle was successfully signaled, and the host server is ready</returns>
        internal static bool SignalHostForStartup(string applicationPhysicalPath, bool blockUntilStarted)
        {
            HostConnectionData connection;

            if (FindHostConnection(applicationPhysicalPath, out connection))
            {
                return SignalHostForStartup(connection, blockUntilStarted);
            }

            return false;
        }

        /// <summary>
        /// Send a signal to the host to make sure it is running.
        /// </summary>
        /// <param name="connection">The connection to signal.</param>
        /// <returns>True if the handle was successfully signaled, and the host server is ready</returns>
        internal static bool SignalHostForStartup(HostConnectionData connection)
        {
            return SignalHostForStartup(connection, blockUntilStarted: true);
        }

        /// <summary>
        /// Send a signal to the host to make sure it is running.
        /// </summary>
        /// <param name="connection">The connection to signal.</param>
        /// <param name="blockUntilStarted">If true, this method will not return until the host signals it is ready</param>
        /// <returns>True if the handle was successfully signaled, and the host server is ready</returns>
        internal static bool SignalHostForStartup(HostConnectionData connection, bool blockUntilStarted)
        {
            if (connection.RequestSignalName != null &&
                connection.ReadySignalName != null)
            {
                SendSignal(connection.RequestSignalName);

                if (blockUntilStarted)
                {
                    return WaitForSignal(connection.ReadySignalName);
                }
                else
                {
                    return true;
                }
            }

            return false;
        }

        private static void SendSignal(string signalName)
        {
            EventWaitHandle signalHandle;

            if (EventWaitHandle.TryOpenExisting(signalName, out signalHandle))
            {
                try
                {
                    signalHandle.Set();
                }
                finally
                {
                    signalHandle.Dispose();
                }
            }
        }

        private static bool WaitForSignal(string signalName)
        {
            EventWaitHandle signalHandle;

            if (EventWaitHandle.TryOpenExisting(signalName, out signalHandle))
            {
                try
                {
                    return signalHandle.WaitOne(HostStartupTimeout);
                }
                finally
                {
                    signalHandle.Dispose();
                }
            }
            else
            {
                // The ready signal is disposed after the host server is started,
                // so if it does not exist, that means the server is already started.
                return true;
            }
        }

        private static bool ConnectionContainsApplication(HostConnectionData connection, string applicationPhysicalPath)
        {
            foreach (string projectPath in connection.ProjectPaths)
            {
                if (PathUtil.CompareNormalizedPaths(applicationPhysicalPath, projectPath))
                {
                    return true;
                }
            }

            return false;
        }

        private static IEnumerable<string> GetAllInstanceFileNames()
        {
            IEnumerable<string> fileNames = Enumerable.Empty<string>();

            foreach (string indexFileName in BrowserLinkConstants.IndexFileNames)
            {
                fileNames = fileNames.Concat(ReadAllLinesFrom(indexFileName));
            }

            return fileNames;
        }

        private static IEnumerable<HostConnectionData> ReadConnectionData(string instanceFileName)
        {
            string version2FileName = instanceFileName + BrowserLinkConstants.Version2Suffix;
            if (MappedFileExists(version2FileName))
            {
                return ReadV2ConnectionData(version2FileName);
            }
            else
            {
                HostConnectionData connectionData;

                if (ReadV1ConnectionData(instanceFileName, out connectionData))
                {
                    return new HostConnectionData[] { connectionData };
                }
                else
                {
                    return new HostConnectionData[0];
                }
            }
        }

        private static bool ReadV1ConnectionData(string instanceFileName, out HostConnectionData connection)
        {
            List<string> lines = ReadAllLinesFrom(instanceFileName);

            return ParseV1ConnectionData(instanceFileName, lines, out connection);
        }

        internal static bool ParseV1ConnectionData(string instanceFileName, List<string> lines, out HostConnectionData connection)
        {
            if (lines.Count > 2)
            {
                string connectionString;
                string sslConnectionString;
                string requestSignalName;
                string readySignalName;
                string injectScriptVerb;
                string mappingDataVerb;
                string serverDataVerb;
                IEnumerable<string> projectPaths;

                CreateConnectionStringsAndProjectPaths(
                    lines,
                    out connectionString,
                    out sslConnectionString,
                    out projectPaths);

                CreateSignalNames(
                    instanceFileName,
                    out requestSignalName,
                    out readySignalName);

                CreateVerbUrls(
                    V1DefaultProperties,
                    connectionString,
                    out injectScriptVerb,
                    out mappingDataVerb,
                    out serverDataVerb);

                connection = new HostConnectionData(
                    connectionString,
                    sslConnectionString,
                    requestSignalName,
                    readySignalName,
                    injectScriptVerb,
                    mappingDataVerb,
                    serverDataVerb,
                    projectPaths);

                return true;
            }

            connection = null;
            return false;
        }

        private static void CreateConnectionStringsAndProjectPaths(List<string> lines, out string connectionString, out string sslConnectionString, out IEnumerable<string> projectPaths)
        {
            connectionString = lines[0];
            sslConnectionString = lines[1];

            List<string> projectPathsList = new List<string>();

            for (int i = 2; i < lines.Count; i++)
            {
                projectPathsList.Add(PathUtil.NormalizeDirectoryPath(lines[i]));
            }

            projectPaths = projectPathsList;
        }

        private static IEnumerable<HostConnectionData> ReadV2ConnectionData(string instanceFileName)
        {
            List<string> instanceFileLines = ReadAllLinesFrom(instanceFileName);

            return ParseV2ConnectionData(instanceFileName, instanceFileLines);
        }

        internal static IEnumerable<HostConnectionData> ParseV2ConnectionData(string instanceFileName, List<string> instanceFileLines)
        {
            string connectionString;
            string sslConnectionString;
            string requestSignalName;
            string readySignalName;
            string injectScriptVerb;
            string mappingDataVerb;
            string serverDataVerb;
            IEnumerable<string> projectPaths;

            string instanceFileNameBase = instanceFileName.Substring(0, instanceFileName.Length - BrowserLinkConstants.Version2Suffix.Length);
            CreateSignalNames(
                instanceFileNameBase,
                out requestSignalName,
                out readySignalName);

            Dictionary<string, string> properties = new Dictionary<string, string>();
            Dictionary<string, string> projects = new Dictionary<string, string>();

            ParseValuesFromLines(instanceFileLines, properties, projects);

            List<HostConnectionData> connections = new List<HostConnectionData>();

            foreach (KeyValuePair<string, string> project in projects)
            {
                CreateConnectionStrings(
                    properties,
                    project.Key,
                    out connectionString,
                    out sslConnectionString);

                CreateVerbUrls(
                    properties,
                    connectionString,
                    out injectScriptVerb,
                    out mappingDataVerb,
                    out serverDataVerb);

                projectPaths = new string[] { PathUtil.NormalizeDirectoryPath(project.Value) };

                connections.Add(new HostConnectionData(
                    connectionString,
                    sslConnectionString,
                    requestSignalName,
                    readySignalName,
                    injectScriptVerb,
                    mappingDataVerb,
                    serverDataVerb,
                    projectPaths));
            }

            return connections;
        }

        private static void CreateConnectionStrings(Dictionary<string, string> properties, string projectKey, out string connectionString, out string sslConnectionString)
        {
            connectionString = String.Empty;
            sslConnectionString = String.Empty;

            string hostName;
            string fetchScriptVerb;
            if (properties.TryGetValue(BrowserLinkConstants.HostNameKey, out hostName) &&
                properties.TryGetValue(BrowserLinkConstants.FetchScriptVerbKey, out fetchScriptVerb))
            {
                string httpPort;
                if (properties.TryGetValue(BrowserLinkConstants.HttpPortKey, out httpPort))
                {
                    connectionString = String.Format("http://{0}:{1}/{2}/{3}",
                        hostName,
                        httpPort,
                        projectKey,
                        fetchScriptVerb);
                }

                string httpsPort;
                if (properties.TryGetValue(BrowserLinkConstants.HttpsPortKey, out httpsPort))
                {
                    sslConnectionString = String.Format("https://{0}:{1}/{2}/{3}",
                        hostName,
                        httpsPort,
                        projectKey,
                        fetchScriptVerb);
                }
            }
        }

        private static void CreateSignalNames(string instanceFileName, out string requestSignalName, out string readySignalName)
        {
            requestSignalName = instanceFileName + BrowserLinkConstants.RequestSignalSuffix;
            readySignalName = instanceFileName + BrowserLinkConstants.ReadySignalSuffix;
        }

        private static void CreateVerbUrls(IReadOnlyDictionary<string, string> properties, string httpConnectionString, out string injectScriptVerb, out string mappingDataVerb, out string serverDataVerb)
        {
            injectScriptVerb = null;
            mappingDataVerb = null;
            serverDataVerb = null;

            if (httpConnectionString != null)
            {
                Uri connectionUri;
                if (Uri.TryCreate(httpConnectionString, UriKind.Absolute, out connectionUri))
                {
                    string verbName;

                    if (properties.TryGetValue(BrowserLinkConstants.InjectScriptVerbKey, out verbName))
                    {
                        injectScriptVerb = new Uri(connectionUri, verbName).ToString();
                    }

                    if (properties.TryGetValue(BrowserLinkConstants.MappingDataVerbKey, out verbName))
                    {
                        mappingDataVerb = new Uri(connectionUri, verbName).ToString();
                    }

                    if (properties.TryGetValue(BrowserLinkConstants.ServerDataVerbKey, out verbName))
                    {
                        serverDataVerb = new Uri(connectionUri, verbName).ToString();
                    }
                }
            }
        }

        private static void ParseValuesFromLines(List<string> lines, Dictionary<string, string> properties, Dictionary<string, string> projects)
        {
            foreach (string line in lines)
            {
                int colonIndex = line.IndexOf(':');
                if (colonIndex < 0)
                {
                    continue;
                }

                string key = line.Substring(0, colonIndex);
                string value = line.Substring(colonIndex + 1);

                if (String.Equals(key, BrowserLinkConstants.ProjectDataKey, StringComparison.Ordinal))
                {
                    string[] parts = value.Split(';');
                    if (parts.Length < 2)
                    {
                        continue;
                    }

                    projects[parts[1]] = parts[0];
                }
                else
                {
                    properties[key] = value;
                }
            }
        }

        private static List<string> ReadAllLinesFrom(string fileName)
        {
            MemoryMappedFile file = null;
            MemoryMappedViewStream stream = null;

            if (MappedFileExists(fileName))
            {
                try
                {
                    file = MemoryMappedFile.OpenExisting(fileName, MemoryMappedFileRights.Read);

                    stream = file.CreateViewStream(0, 0, MemoryMappedFileAccess.Read);

                    return ReadAllLinesFrom(stream);
                }
                catch (FileNotFoundException)
                {
                    // File does not exist
                }
                finally
                {
                    if (stream != null)
                    {
                        stream.Dispose();
                    }

                    if (file != null)
                    {
                        file.Dispose();
                    }
                }
            }

            // Failure, return an empty set
            return new List<string>();
        }

        private static List<string> ReadAllLinesFrom(MemoryMappedViewStream stream)
        {
            List<string> lines = new List<string>();

            StreamReader reader = new StreamReader(stream, Encoding.UTF8);

            // A value of 0 indicates the end of valid content in the file.
            // reader.Peek() returns <0 when it reaches the end of the stream.
            while (reader.Peek() > 0)
            {
                lines.Add(reader.ReadLine());
            }

            return lines;
        }

        /// <summary>
        /// Use P/Invoke methods to check for the existance of the mapped file. This
        /// is faster than letting MemoryMappedFile.OpenExisting fail, because that
        /// method throws an exception.
        /// </summary>
        private static bool MappedFileExists(string fileName)
        {
            IntPtr handle = NativeMethods.OpenFileMapping(4 /* FILE_MAP_READ */, false, fileName);

            if (handle == IntPtr.Zero)
            {
                return false;
            }
            else
            {
                NativeMethods.CloseHandle(handle);

                return true;
            }
        }
    }
}
