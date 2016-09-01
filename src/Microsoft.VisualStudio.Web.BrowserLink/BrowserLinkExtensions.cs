// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.VisualStudio.Web.BrowserLink;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Implementation of extension methods for configuring Browser Link
    /// in an ASP.NET Core application.
    /// </summary>
    public static class BrowserLinkExtensions
    {
        /// <summary>
        /// This method is called to enable Browser Link in an application. It
        /// registers a factory method that creates BrowserLinkMiddleware for
        /// each request.
        /// </summary>
        public static IApplicationBuilder UseBrowserLink(this IApplicationBuilder app)
        {
            if (!IsMicrosoftRuntime)
            {
                return app;
            }

            try
            {
                string applicationBasePath;

                if (GetApplicationBasePath(app, out applicationBasePath))
                {
                    applicationBasePath = PathUtil.NormalizeDirectoryPath(applicationBasePath);

                    HostConnectionUtil.SignalHostForStartup(applicationBasePath, blockUntilStarted: false);

                    BrowserLinkMiddlewareFactory factory = new BrowserLinkMiddlewareFactory(applicationBasePath);

                    return app.Use(factory.CreateBrowserLinkMiddleware);
                }
                else
                {
                    // Browser Link doesn't work if we don't have an application path
                    return app;
                }
            }
            catch
            {
                // Something went wrong initializing the runtime. Browser Link won't work. 
                return app;
            }
        }

        private static bool IsMicrosoftRuntime
        {
            get { return Type.GetType("Mono.Runtime") == null; }
        }

        private static bool GetApplicationBasePath(IApplicationBuilder app, out string applicationBasePath)
        {
            IHostingEnvironment hostingEnvironment = app.ApplicationServices.GetService(typeof(IHostingEnvironment)) as IHostingEnvironment;

            if (hostingEnvironment != null)
            {
                applicationBasePath = hostingEnvironment.ContentRootPath;

                return applicationBasePath != null;
            }

            applicationBasePath = null;
            return false;
        }
    }
}
