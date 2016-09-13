using Microsoft.AspNetCore.Http;
using System;
using Microsoft.AspNetCore.Http.Headers;
using Microsoft.Net.Http.Headers;
using System.Collections.Generic;

namespace Microsoft.VisualStudio.Web.BrowserLink
{
    internal static class BrowserLinkMiddleWareUtil
    {
        internal static int GetRequestPort(IHeaderDictionary headers)
        {
            RequestHeaders requestHeader = new RequestHeaders(headers);

            foreach (EntityTagHeaderValue value in requestHeader.IfNoneMatch)
            {
                string[] strings = value.ToString().Split(':');

                if (strings.Length >= 2)
                {
                    return Int32.Parse(strings[1].Substring(0, strings[1].Length - 1));
                }
            }

            return -1;
        }

        internal static int GetCurrentPort(string connectionString)
        {
            string[] strings1 = connectionString.Split(':');

            if (strings1.Length >= 3)
            {
                string[] strings2 = strings1[2].Split('/');

                return Int32.Parse(strings2[0]);
            }

            return -1;
        }

        internal static void RemoveETagAndTimeStamp(IHeaderDictionary headers)
        {
            RequestHeaders requestHeader = new RequestHeaders(headers);

            requestHeader.IfNoneMatch = null;
            requestHeader.IfModifiedSince = null;
        }

        internal static void DeletePortFromETag(IHeaderDictionary headers)
        {
            RequestHeaders requestHeader = new RequestHeaders(headers);
            string newETag = "";
            IList<EntityTagHeaderValue> list = requestHeader.IfNoneMatch;

            foreach(EntityTagHeaderValue value in list)
            {
                String[] strings = value.ToString().Split(':');

                if (strings.Length >= 2)
                {
                    newETag = strings[0] + "\"";
                    break;
                }
            }

            if (newETag.Length > 0)
            {
                list[0] = new EntityTagHeaderValue(newETag);
                requestHeader.IfNoneMatch = list;
            }
        }

        internal static void AddToETag(IHeaderDictionary headers, int port)
        {
            ResponseHeaders responseHeader = new ResponseHeaders(headers);
      
            if (responseHeader.ETag != null)
            {
                string temp = responseHeader.ETag.ToString().Substring(0, responseHeader.ETag.ToString().Length - 1) + ":" + port + "\"";
                responseHeader.ETag = new EntityTagHeaderValue(temp);
            }
        }

        internal static int FilterRequestHeader(IHeaderDictionary headers, string connectionString)
        {
            int requestPort = GetRequestPort(headers);
            int currentPort = GetCurrentPort(connectionString);

            if (requestPort != currentPort)
            {
                RemoveETagAndTimeStamp(headers);
            }
            else
            {
                DeletePortFromETag(headers);
            }

            return currentPort;
        }
    }
}
