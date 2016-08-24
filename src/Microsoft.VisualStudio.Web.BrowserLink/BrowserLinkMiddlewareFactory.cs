// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;

namespace Microsoft.VisualStudio.Web.BrowserLink
{
    /// <summary>
    /// An instance of this class is created when Browser Link is registered
    /// in an application. It's job is to remember the base path of the
    /// application, so it can be passed on to the BrowserLinkMiddleware.
    /// </summary>
    internal class BrowserLinkMiddlewareFactory
    {
        private string _applicationBasePath;

        internal BrowserLinkMiddlewareFactory(string applicationBasePath)
	    {
            _applicationBasePath = applicationBasePath;
	    }

        public RequestDelegate CreateBrowserLinkMiddleware(RequestDelegate next)
        {
            BrowserLinkMiddleware middleware = new BrowserLinkMiddleware(_applicationBasePath, next);

            return middleware.Invoke;
        }
    }
}