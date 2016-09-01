// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.VisualStudio.Web.BrowserLink
{
    /// <summary>
    /// Represents an host connection published from a design tool's host process.
    /// </summary>
    internal class HostConnectionData
    {
        internal HostConnectionData(string connectionString, string sslConnectionString, string requestSignalName, string readySignalName, IEnumerable<string> projectPaths)
        {
            ConnectionString = connectionString;
            SslConnectionString = sslConnectionString;
            RequestSignalName = requestSignalName;
            ReadySignalName = readySignalName;
            ProjectPaths = projectPaths;
        }

        /// <summary>
        /// The name of the event to signal when you want the host to start.
        /// </summary>
        public string RequestSignalName { get; private set; }

        /// <summary>
        /// The name of the event to wait on after requesting the host to start.
        /// </summary>
        public string ReadySignalName { get; private set; }

        /// <summary>
        /// The string used to identify the host connection.
        /// </summary>
        public string ConnectionString { get; private set; }

        /// <summary>
        /// The string used to identify the SSL host connection.
        /// </summary>
        public string SslConnectionString { get; private set; }

        /// <summary>
        /// The physical paths of projects loaded in this instance of the design tool.
        /// </summary>
        public IEnumerable<string> ProjectPaths { get; private set; }
    }
}
