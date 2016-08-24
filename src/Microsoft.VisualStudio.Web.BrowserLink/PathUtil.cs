// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.VisualStudio.Web.BrowserLink
{
    /// <summary>
    /// Helpers for dealing with file paths
    /// </summary>
    internal static class PathUtil
    {
        /// <summary>
        /// Takes a path, and returns an equivalent path in a format that can
        /// be used for comparisons.
        /// </summary>
        public static string NormalizeDirectoryPath(string path)
        {
            if (path.Contains("/"))
            {
                path = path.Replace('/', '\\');
            }

            if (!path.EndsWith("\\"))
            {
                path += "\\";
            }

            return path.ToLowerInvariant();
        }

        /// <summary>
        /// Compares two paths, assuming they were both produced by NormalizeDirectoryPath
        /// </summary>
        /// <returns>True if the paths are equivalent</returns>
        public static bool CompareNormalizedPaths(string path1, string path2)
        {
            return String.Equals(path1, path2, StringComparison.Ordinal);
        }
    }
}