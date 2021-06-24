﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

[assembly: HostingStartup(typeof(Microsoft.VisualStudio.Web.BrowserLink.HostingStartup))]

namespace Microsoft.VisualStudio.Web.BrowserLink
{
    internal sealed class HostingStartup : IHostingStartup, IStartupFilter
    {
        public void Configure(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services => services.TryAddEnumerable(ServiceDescriptor.Singleton<IStartupFilter>(this)));
        }

        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            return app =>
            {
                app.UseBrowserLink();
                next(app);
            };
        }
    }
}
