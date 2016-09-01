// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Web.BrowserLink
{
    internal static class StaticTaskResult
    {
        public static readonly Task<bool> True = Task.FromResult(true);
        public static readonly Task<bool> False = Task.FromResult(false);
        public static readonly Task<string> NullString = Task.FromResult((string)null);
        public static readonly Task Complete = True;

        public static readonly Task<int> HttpInternalServerError = Task.FromResult(500);

        public static readonly Task<int> Zero = Task.FromResult(0);
    }
}
