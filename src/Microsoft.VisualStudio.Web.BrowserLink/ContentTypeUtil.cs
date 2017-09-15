// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.VisualStudio.Web.BrowserLink
{
    /// <summary>
    /// Helpers for testing the content type of the response content.
    /// </summary>
    internal static class ContentTypeUtil
    {
        private const string HtmlContentType = "text/html";
        private const string XhtmlContentType = "application/xhtml+xml";

        public static bool IsSupportedContentTypes(string contentType)
        {
            if (String.IsNullOrEmpty(contentType))
            {
                return false;
            }

            string[] parts = contentType.Split(';');

            return String.Equals(parts[0].Trim(), HtmlContentType, StringComparison.OrdinalIgnoreCase) || String.Equals(parts[0].Trim(), XhtmlContentType, StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsHtml(string requestUrl, byte[] buffer, int offset, int count)
        {
            IntPtr realContentTypePtr = IntPtr.Zero;

            try
            {
                if (offset != 0)
                {
                    byte[] originalBuffer = buffer;

                    if (count > 512)
                    {
                        count = 512;
                    }

                    buffer = new byte[count];

                    Array.Copy(originalBuffer, offset, buffer, 0, count);
                }

                int ret = NativeMethods.FindMimeFromData(IntPtr.Zero, requestUrl, buffer, buffer.Length, null, 0, out realContentTypePtr, 0);

                if (ret == 0 && realContentTypePtr != IntPtr.Zero)
                {
                    return IsSupportedContentTypes(Marshal.PtrToStringUni(realContentTypePtr));
                }
                else
                {
                    return false;
                }
            }
            finally
            {
                if (realContentTypePtr != IntPtr.Zero)
                {
                    Marshal.FreeCoTaskMem(realContentTypePtr);
                }
            }

        }
    }
}