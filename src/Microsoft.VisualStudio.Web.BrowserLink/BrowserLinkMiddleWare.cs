// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Headers;

namespace Microsoft.VisualStudio.Web.BrowserLink
{
    /// <summary>
    /// This class is created once, and invoked for each request. It puts a filter on
    /// the Response.Body, which will inject Browser Link's script links into the
    /// content (if the content is HTML).
    /// </summary>
    internal class BrowserLinkMiddleware
    {
        // Number of timeouts allowed before we stop trying to connect to the host
        private const int FilterRequestTimeoutLimit = 2;

        // Number of timeouts that occurred while attempting to connect to the host
        private static int _filterRequestTimeouts = 0;

        private RequestDelegate _next;
        private string _applicationPath;

        internal BrowserLinkMiddleware(string applicationPath, RequestDelegate next)
        {
            _applicationPath = applicationPath;
            _next = next;
        }

        /// <summary>
        /// This method is called to process the response.
        /// </summary>
        internal Task Invoke(HttpContext context)
        {
            string requestId = Guid.NewGuid().ToString("N");

            IHttpSocketAdapter injectScriptSocket = GetSocketConnectionToHost(_applicationPath, requestId, "injectScriptLink", context.Request.IsHttps);

            if (injectScriptSocket != null)
            {
                return ExecuteWithFilter(injectScriptSocket, requestId, context);
            }
            else
            {
                RequestHeaders requestHeader = new RequestHeaders(context.Request.Headers); 

                if (requestHeader.IfNoneMatch != null && BrowserLinkMiddleWareUtil.GetRequestPort(requestHeader).Count != 0)
                {
                    BrowserLinkMiddleWareUtil.RemoveETagAndTimeStamp(requestHeader);
                }

                return ExecuteWithoutFilter(context);
            }
        }

        private PageExecutionListenerFeature AddPageExecutionListenerFeatureTo(HttpContext context, string requestId)
        {
            IHttpSocketAdapter mappingDataSocket = GetSocketConnectionToHost(_applicationPath, requestId, "sendMappingData", context.Request.IsHttps);

            if (mappingDataSocket != null)
            {
                PageExecutionListenerFeature listener = new PageExecutionListenerFeature(mappingDataSocket);

                context.Features.Set(listener);

                return listener;
            }

            return null;
        }

        private async Task ExecuteWithFilter(IHttpSocketAdapter injectScriptSocket, string requestId, HttpContext httpContext)
        {
            ScriptInjectionFilterContext filterContext = new ScriptInjectionFilterContext(httpContext);
            int currentPort = -1;

            PreprocessRequestHeader(httpContext, ref currentPort);

            RequestHeaders requestHeader = new RequestHeaders(httpContext.Request.Headers);

            if (currentPort == -1)
            {
                BrowserLinkMiddleWareUtil.RemoveETagAndTimeStamp(requestHeader);
            }

            using (ScriptInjectionFilterStream filter = new ScriptInjectionFilterStream(injectScriptSocket, filterContext))
            {
                httpContext.Response.Body = filter;
                httpContext.Response.OnStarting(delegate ()
                {
                    httpContext.Response.ContentLength = null;
                    ResponseHeaders responseHeader = new ResponseHeaders(httpContext.Response.Headers);

                    BrowserLinkMiddleWareUtil.AddToETag(responseHeader, currentPort);

                    return StaticTaskResult.True;
                });

                using (AddPageExecutionListenerFeatureTo(httpContext, requestId))
                {
                    await _next(httpContext);

                    await filter.WaitForFilterComplete();

                    if (filter.ScriptInjectionTimedOut)
                    {
                        _filterRequestTimeouts++;
                    }
                    else
                    {
                        _filterRequestTimeouts = 0;
                    }
                }
            }
        }

        private Task ExecuteWithoutFilter(HttpContext context)
        {
            return _next(context);
        }

        private static IHttpSocketAdapter GetSocketConnectionToHost(string applicationPath, string requestId, string rpcMethod, bool isHttps)
        {
            // The host should send an initial response immediately after
            // the connection is established. If it fails to do so multiple times,
            // stop trying. Each timeout is delaying a response to the browser.
            //
            // This will only reset when the server process is restarted.
            if (_filterRequestTimeouts >= FilterRequestTimeoutLimit)
            {
                return null;
            }

            if (FindAndSignalHostConnection(applicationPath))
            {
                return new DelayConnectingHttpSocketAdapter(async delegate()
                {
                    Uri connectionString;

                    if (GetHostConnectionString(applicationPath, out connectionString))
                    {
                        IHttpSocketAdapter httpSocket = await HttpSocketAdapter.OpenHttpSocketAsync("GET", new Uri(connectionString, rpcMethod));

                        AddRequestHeaders(httpSocket, requestId, isHttps);

                        return httpSocket;
                    }

                    return null;
                });
            }

            return null;
        }

        private static void AddRequestHeaders(IHttpSocketAdapter httpSocket, string requestId, bool isHttps)
        {
            httpSocket.AddRequestHeader(BrowserLinkConstants.RequestIdHeaderName, requestId);

            if (isHttps)
            {
                httpSocket.AddRequestHeader(BrowserLinkConstants.RequestScheme, "https");
            }
            else
            {
                httpSocket.AddRequestHeader(BrowserLinkConstants.RequestScheme, "http");
            }
        }

        private static bool FindAndSignalHostConnection(string applicationPath)
        {
            HostConnectionData connectionData;

            if (HostConnectionUtil.FindHostConnection(applicationPath, out connectionData))
            {
                return HostConnectionUtil.SignalHostForStartup(connectionData, blockUntilStarted: false);
            }

            return false;
        }

        private static bool GetHostConnectionString(string applicationPath, out Uri connectionString)
        {
            HostConnectionData connectionData;

            if (GetHostConnectionData(applicationPath, out connectionData))
            {
                return Uri.TryCreate(connectionData.ConnectionString, UriKind.Absolute, out connectionString);
            }

            connectionString = null;
            return false;
        }

        private static bool GetHostConnectionData(string applicationPath, out HostConnectionData connectionData)
        {
            if (HostConnectionUtil.FindHostConnection(applicationPath, out connectionData))
            {
                return EnsureHostServerStarted(applicationPath, ref connectionData);
            }

            connectionData = null;
            return false;
        }

        private static bool EnsureHostServerStarted(string applicationPath, ref HostConnectionData connectionData)
        {
            if (String.IsNullOrEmpty(connectionData.ConnectionString))
            {
                if (!HostConnectionUtil.SignalHostForStartup(connectionData))
                {
                    return false;
                }

                if (!HostConnectionUtil.FindHostConnection(applicationPath, out connectionData))
                {
                    return false;
                }

                if (String.IsNullOrEmpty(connectionData.ConnectionString))
                {
                    return false;
                }
            }

            return true;
        }

        private void PreprocessRequestHeader(HttpContext httpContext, ref int currentPort)
        {
            RequestHeaders requestHeader = new RequestHeaders(httpContext.Request.Headers);

            if (requestHeader.IfNoneMatch != null)
            {
                HostConnectionData connectionData;

                if (GetHostConnectionData(_applicationPath, out connectionData))
                {
                    currentPort = BrowserLinkMiddleWareUtil.FilterRequestHeader(requestHeader, connectionData.ConnectionString);
                }
            }
        }
    }
}