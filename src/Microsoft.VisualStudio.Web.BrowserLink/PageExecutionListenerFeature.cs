// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.VisualStudio.Web.BrowserLink
{
    /// <summary>
    /// This feature is provided for page renderers, so they can provide mapping
    /// data as the page renders.
    /// </summary>
    internal class PageExecutionListenerFeature : IDisposable
    {
        private MappingDataWriter _mappingDataWriter;
        private List<TextWriterDecorator> _writers = new List<TextWriterDecorator>();

        internal PageExecutionListenerFeature(IHttpSocketAdapter mappingDataSocket)
        {
            _mappingDataWriter = new MappingDataWriter(mappingDataSocket);
        }

        /// <summary>
        /// This method is called to give the listener a chance to replace the TextWriter
        /// used to render the page.
        /// </summary>
        /// <param name="writer">The original TextWriter for the output stream</param>
        /// <returns>A TextWriter that will be used for writing the output.</returns>
        public TextWriter DecorateWriter(TextWriter writer)
        {
            try
            { 
                TextWriterDecorator decorator = new TextWriterDecorator(writer, this, _writers.Count);

                foreach (TextWriterDecorator existingWriter in _writers)
                {
                    AddTextRelationship(existingWriter, decorator);
                }

                _writers.Add(decorator);

                return decorator;
            }
            catch
            {
                return writer;
            }
        }

        /// <summary>
        /// Create a context that associates a TextWriter with a source file.
        /// </summary>
        /// <param name="sourceFilePath">The path to the source file.</param>
        /// <param name="writer">The TextWriter used to render the source file</param>
        /// <returns>A context that will be called back with mapping data within the source file.</returns>
        public PageExecutionContext GetContext(string sourceFilePath, TextWriter writer)
        {
            return new PageExecutionContext(_mappingDataWriter, sourceFilePath, writer);
        }

        internal void AddTextRelationship(TextWriterDecorator copyingToWriter, TextWriterDecorator copyingFromWriter)
        {
            _mappingDataWriter.WriteTextRelationship(copyingToWriter.RenderedOutputIndex, copyingFromWriter.RenderedOutputIndex, copyingToWriter.OutputPosition);
        }

        public void Dispose()
        {
            if (_mappingDataWriter != null)
            {
                try
                {
                    SendEndOfData();

                    _mappingDataWriter.Dispose();
                }
                catch { }
                finally
                {
                    _mappingDataWriter = null;
                }
            }
        }

        private void SendEndOfData()
        {
            foreach (TextWriterDecorator writer in _writers)
            {
                _mappingDataWriter.WriteOutputDefinition(writer.RenderedOutputIndex, writer.RenderedOutput);
            }

            _mappingDataWriter.WriteEndOfData();
        }
    }
}