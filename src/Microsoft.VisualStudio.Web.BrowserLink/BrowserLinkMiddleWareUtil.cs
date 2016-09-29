using System;
using Microsoft.AspNetCore.Http.Headers;
using Microsoft.Net.Http.Headers;
using System.Collections.Generic;

namespace Microsoft.VisualStudio.Web.BrowserLink
{
    internal static class BrowserLinkMiddleWareUtil
    {
        internal static List<int> GetRequestPort(RequestHeaders requestHeader)
        {
            List<int> requestPortList = new List<int>();

            if (requestHeader.IfNoneMatch != null)
            {
                for (int index = 0; index < requestHeader.IfNoneMatch.Count; ++index)
                {
                    string[] strings = requestHeader.IfNoneMatch[index].ToString().Split(':');

                    if (strings.Length >= 2)
                    {
                        int port = -1;

                        if (Int32.TryParse(strings[1].Substring(0, strings[1].Length - 1), out port))
                        {
                            requestPortList.Add(port);
                        }
                    }
                }
            }

            return requestPortList;
        }

        internal static int GetCurrentPort(string connectionString)
        {
            Uri uri;

            if (connectionString == null || !Uri.TryCreate(connectionString, UriKind.Absolute, out uri))
            {
                return -1;
            }

            return uri.Port;
        }

        internal static void RemoveETagAndTimeStamp(RequestHeaders requestHeader)
        {
            requestHeader.IfNoneMatch = null;
            requestHeader.IfModifiedSince = null;
        }

        internal static void DeletePortFromETag(RequestHeaders requestHeader)
        {
            string newETag = "";
            IList<EntityTagHeaderValue> list = requestHeader.IfNoneMatch;

            for (int index = 0; index < list.Count; ++index)
            {
                String[] strings = list[index].ToString().Split(':');

                if (strings.Length >= 2)
                {
                    newETag = strings[0] + "\"";
                    list[index] = new EntityTagHeaderValue(newETag);
                }
            }

            requestHeader.IfNoneMatch = list;
        }

        internal static void AddToETag(ResponseHeaders responseHeader, int port)
        {
            if (responseHeader.ETag != null)
            {
                string temp = responseHeader.ETag.ToString().Substring(0, responseHeader.ETag.ToString().Length - 1) + ":" + port + "\"";
                responseHeader.ETag = new EntityTagHeaderValue(temp);
            }
        }

        internal static bool IfMatch(List<int> requestPortList, int currentPort)
        {
            foreach (int port in requestPortList)
            {
                if (port == currentPort)
                {
                    return true;
                }
            }

            return false;
        }

        internal static int FilterRequestHeader(RequestHeaders requestHeader, string connectionString)
        {
            List<int> requestPortList = GetRequestPort(requestHeader);
            int currentPort = GetCurrentPort(connectionString);

            if (!IfMatch(requestPortList, currentPort))
            {
                RemoveETagAndTimeStamp(requestHeader);
            }
            else
            {
                DeletePortFromETag(requestHeader);
            }

            return currentPort;
        }
    }
}
