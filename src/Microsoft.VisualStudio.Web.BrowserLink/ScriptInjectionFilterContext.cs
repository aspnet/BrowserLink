// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using Microsoft.AspNetCore.Http;

namespace Microsoft.VisualStudio.Web.BrowserLink
{
    /// <summary>
    /// IScriptInjectionFilterContext is an abstraction of the request data required
    /// by ScriptInjectionFilterStream. It is needed for unit testing, and eventually
    /// so ASP.NET and ProjectK can share filter code even though they have different
    /// HttpContext classes.
    /// </summary>
    internal interface IScriptInjectionFilterContext
    {
        /// <summary>
        /// The local (app-relative) path of the request.
        /// </summary>
        string RequestPath { get; }

        /// <summary>
        /// The response body, where filtered content will be written.
        /// </summary>
        Stream ResponseBody { get; }

        /// <summary>
        /// The Content-Type that is being returned with the response.
        /// </summary>
        string ResponseContentType { get; }
    }

    internal class ScriptInjectionFilterContext : IScriptInjectionFilterContext
    {
        private HttpContext _httpContext;

        internal ScriptInjectionFilterContext(HttpContext httpContext)
        {
            _httpContext = httpContext;
        }

        public string RequestPath
        {
            get
            {
                return _httpContext.Request.Path.HasValue
                    ? _httpContext.Request.Path.ToString()
                    : null;
            }
        }

        public Stream ResponseBody
        {
            get { return _httpContext.Response.Body; }
        }

        public string ResponseContentType
        {
            get { return _httpContext.Response.ContentType; }
        }
    }
}