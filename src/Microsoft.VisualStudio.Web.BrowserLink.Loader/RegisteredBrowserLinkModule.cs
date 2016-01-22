// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.PlatformAbstractions;

namespace Microsoft.VisualStudio.Web.BrowserLink.Loader
{
    /// <summary>
    /// Represents one Browser Link middleware assembly found on the machine, and
    /// provides the ability to call methods on that middleware through reflection.
    /// </summary>
    internal class RegisteredBrowserLinkModule
    {
        // Reflection information about the UseBrowserLink method
        private const string UseBrowserLinkMethodName = "UseBrowserLink";
        private static readonly Type[] UseBrowserLinkParameters = new Type[] { typeof(IApplicationBuilder) };

        internal RegisteredBrowserLinkModule(string name, Version version, string assemblyPath, string extensionTypeName)
        {
            Name = name;
            Version = version;
            AssemblyPath = assemblyPath;
            ExtensionTypeName = extensionTypeName;
        }

        /// <summary>
        /// A unique name identifying this module.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// The build version of this module.
        /// </summary>
        public Version Version { get; private set; }

        /// <summary>
        /// The path to the assembly implementing this module.
        /// </summary>
        public string AssemblyPath { get; private set; }

        /// <summary>
        /// The type name (including namespace) of the type that implements methods
        /// that should be invoked using reflection.
        /// </summary>
        public string ExtensionTypeName { get; private set; }

        /// <summary>
        /// Load the ExtensionType, and call the UseBrowserLink method on that
        /// type. This will configure Browser Link for the application.
        /// </summary>
        /// <param name="app">The application being configured.</param>
        /// <returns><paramref name="app"/></returns>
        public IApplicationBuilder InvokeUseBrowserLink(IApplicationBuilder app)
        {
            MethodInfo useBrowserLinkMethod;

            if (GetUseBrowserLinkExtensionMethod(app, out useBrowserLinkMethod))
            {
                return (IApplicationBuilder)useBrowserLinkMethod.Invoke(null, new object[] { app });
            }

            return app;
        }

        private bool GetUseBrowserLinkExtensionMethod(IApplicationBuilder app, out MethodInfo useBrowserLinkMethod)
        {
            return GetExtensionMethod(app, UseBrowserLinkMethodName, UseBrowserLinkParameters, out useBrowserLinkMethod);
        }

        private bool GetExtensionMethod(IApplicationBuilder app, string methodName, Type[] parameterTypes, out MethodInfo method)
        {
            Type extensionType;

            if (GetExtensionType(app, out extensionType))
            {
                method = extensionType.GetRuntimeMethod(methodName, parameterTypes);

                return method != null;
            }
            else
            {
                method = null;
                return false;
            }
        }

        private bool GetExtensionType(IApplicationBuilder app, out Type extensionType)
        {
            Assembly runtimeAssembly;

            if (LoadRuntimeAssembly(app, out runtimeAssembly))
            {
                extensionType = runtimeAssembly.GetType(ExtensionTypeName);

                return extensionType != null;
            }
            else
            {
                extensionType = null;
                return false;
            }
        }

        private bool LoadRuntimeAssembly(IApplicationBuilder app, out Assembly runtimeAssembly)
        {
            IAssemblyLoadContextAccessor loadContextAccessor = app.ApplicationServices.GetService(typeof(IAssemblyLoadContextAccessor)) as IAssemblyLoadContextAccessor;
            
            if (loadContextAccessor != null) 
            {
                IAssemblyLoadContext loadContext = loadContextAccessor.GetLoadContext(this.GetType().GetTypeInfo().Assembly);
                
                if (loadContext != null)
                {
                    runtimeAssembly = loadContext.LoadFile(AssemblyPath);
                      
                    return runtimeAssembly != null;
                }
            }
            
            runtimeAssembly = null;
            return false;
        }
    }
}