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
                HostConnectionData connectionCandidate;

                if (ReadConnectionData(instanceFileName, out connectionCandidate))
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

        private static bool ReadConnectionData(string instanceFileName, out HostConnectionData connection)
        {
            List<string> lines = ReadAllLinesFrom(instanceFileName);

            if (lines.Count > 2)
            {
                string connectionString = lines[0];
                string sslConnectionString = lines[1];
                string requestSignalName = instanceFileName + BrowserLinkConstants.RequestSignalSuffix;
                string readySignalName = instanceFileName + BrowserLinkConstants.ReadySignalSuffix;
                List<string> projects = new List<string>();

                for (int i = 2; i < lines.Count; i++)
                {
                    projects.Add(PathUtil.NormalizeDirectoryPath(lines[i]));
                }

                connection = new HostConnectionData(connectionString, sslConnectionString, requestSignalName, readySignalName, projects);
                return true;
            }

            connection = null;
            return false;
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
