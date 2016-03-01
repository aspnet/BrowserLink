// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Win32;

namespace Microsoft.VisualStudio.Web.BrowserLink.Loader
{
    /// <summary>
    /// Support to look up information about Browser Link modules from the registry.
    /// Browser Link modules are registered under RegistryUtil.BrowserLinkRegistryKey
    /// as follows:
    /// 
    /// [HKEY_LOCAL_MACHINE\(RegistryUtil.BrowserLinkRegistryKey)\(Unique-Name-For-Module)]
    /// "dnxcore50"="Path\to\assembly\for\Core\CLR"
    /// "dnx451"="Path\to\assembly\for\Desktop\CLR"
    /// "ExtensionType"="Namespace.TypeName"
    /// "Version"="WTE Version"
    /// </summary>
    internal static class RegistryUtil
    {
        // The key, under HKLM, where Browser Link modules are registered
        private const string BrowserLinkRegistryKey = @"SOFTWARE\Microsoft\Browser Link";

        // Names of values under each registration key
        private static readonly string VersionName = "Version";
        private static readonly string TypeName = "ExtensionType";

#if NETSTANDARDAPP1_5
        private static readonly string PathName = "dnxcore50";
#elif DNX451
        private static readonly string PathName = "dnx451";
#endif

        /// <summary>
        /// Return the module from the registry with the highest version number.
        /// Modules should be backward-compatible, so that the highest-version
        /// module can handle requests from all SxS installed versions of WTE.
        /// </summary>
        /// <returns>A browser link module, or null if no modules were found.</returns>
        public static RegisteredBrowserLinkModule FindPreferredBrowserLinkModule()
        {
            List<RegisteredBrowserLinkModule> runtimes = FindAllBrowserLinkModules();

            if (runtimes.Count >= 1)
            {
                runtimes.Sort(CompareBrowserLinkModulesByVersion);

                return runtimes[0];
            }

            return null;
        }

        private static List<RegisteredBrowserLinkModule> FindAllBrowserLinkModules()
        {
            List<RegisteredBrowserLinkModule> runtimes = new List<RegisteredBrowserLinkModule>();

            RegistryKey rootKey = Registry.LocalMachine.OpenSubKey(BrowserLinkRegistryKey);
            if (rootKey != null)
            {
                foreach (string subKeyName in rootKey.GetSubKeyNames())
                {
                    RegistryKey subKey = rootKey.OpenSubKey(subKeyName);

                    if (subKey != null)
                    {
                        RegisteredBrowserLinkModule runtime = ReadModuleInformation(subKeyName, subKey);

                        if (runtime != null)
                        {
                            runtimes.Add(runtime);
                        }
                    }
                }
            }

            return runtimes;
        }

        private static int CompareBrowserLinkModulesByVersion(RegisteredBrowserLinkModule x, RegisteredBrowserLinkModule y)
        {
            // Sort in reverse order of version
            return y.Version.CompareTo(x.Version);
        }

        private static RegisteredBrowserLinkModule ReadModuleInformation(string subKeyName, RegistryKey subKey)
        {
            Version version;
            string path;
            string typeName;

            if (TryGetVersionValue(subKey, VersionName, out version) &&
                TryGetStringValue(subKey, PathName, out path) &&
                TryGetStringValue(subKey, TypeName, out typeName))
            {
                return new RegisteredBrowserLinkModule(subKeyName, version, path, typeName);
            }

            return null;
        }

        private static bool TryGetVersionValue(RegistryKey key, string name, out Version version)
        {
            string versionString;

            if (TryGetStringValue(key, name, out versionString))
            {
                return Version.TryParse(versionString, out version);
            }
            else
            {
                version = null;
                return false;
            }
        }

        private static bool TryGetStringValue(RegistryKey key, string name, out string value)
        {
            object untypedValue = key.GetValue(name, null);
            if (untypedValue != null &&
                key.GetValueKind(name) == RegistryValueKind.String)
            {
                value = (string)untypedValue;
                return value != null;
            }
            else
            {
                value = null;
                return false;
            }
        }
    }
}