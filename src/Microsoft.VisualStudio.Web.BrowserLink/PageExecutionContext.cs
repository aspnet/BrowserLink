// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;

namespace Microsoft.VisualStudio.Web.BrowserLink
{
    /// <summary>
    /// A PageExecutionContext is created for each source file that is executed as
    /// part of the page. It remembers the path to the source file and the TextWriter
    /// being used, to pass on during BeginContext and EndContext calls.
    /// </summary>
    internal class PageExecutionContext
    {
        private string _sourceFilePath;
        private TextWriterDecorator _writer;
        private MappingDataWriter _mappingDataWriter;

        public PageExecutionContext(MappingDataWriter mappingDataWriter, string sourceFilePath, TextWriter writer)
        {
            _mappingDataWriter = mappingDataWriter;
            _sourceFilePath = sourceFilePath;
            _writer = writer as TextWriterDecorator;
        }

        /// <summary>
        /// Specifies the part of the source file that is currently being rendered.
        /// </summary>
        /// <param name="position">The start position of the range in the source file</param>
        /// <param name="length">The length of the range in the source file</param>
        /// <param name="isLiteral">True if the range is being written verbatim from the source file</param>
        public void BeginContext(int position, int length, bool isLiteral)
        {
            try
            { 
                if (_writer != null)
                {
                    _mappingDataWriter.WriteBeginContext(position, length, isLiteral, _sourceFilePath, _writer.RenderedOutputIndex, _writer.OutputPosition);
                }
            }
            catch
            { }
        }

        /// <summary>
        /// Specifies that we are done rendering the part of the source file from 
        /// BeginContext. Begin/End context calls can be nested within the same file.
        /// </summary>
        public void EndContext()
        {
            try
            {
                if (_writer != null)
                {
                    _mappingDataWriter.WriteEndContext(_writer.RenderedOutputIndex, _writer.OutputPosition);
                }
            }
            catch
            { }
        }
    }
}