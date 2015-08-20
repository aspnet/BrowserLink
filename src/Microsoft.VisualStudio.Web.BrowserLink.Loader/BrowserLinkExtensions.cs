// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.Web.BrowserLink.Loader;

namespace Microsoft.AspNet.Builder
{
    public static class BrowserLinkLoaderExtensions
    {
        /// <summary>
        /// Configures the application to connect with Visual Studio using
        /// Browser Link.
        /// </summary>
        /// <param name="app"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseBrowserLink(this IApplicationBuilder app)
        {
            try
            { 
                if (!IsMicrosoftRuntime)
                {
                    return app;
                }

                RegisteredBrowserLinkModule preferredRuntime = RegistryUtil.FindPreferredBrowserLinkModule();

                if (preferredRuntime != null)
                {
                    return preferredRuntime.InvokeUseBrowserLink(app);
                }
            }
            catch
            {
                // Bug 1192984: Browser Link couldn't be loaded for some reason, so it
                // will be disabled. Let the app continue working.
            }

            return app;
        }

        private static bool IsMicrosoftRuntime
        {
            get { return Type.GetType("Mono.Runtime") == null; }
        }
    }
}
