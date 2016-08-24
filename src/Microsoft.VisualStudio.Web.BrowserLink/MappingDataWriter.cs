// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;

namespace Microsoft.VisualStudio.Web.BrowserLink
{
    /// <summary>
    /// This feature is provided for page renderers, so they can provide mapping
    /// data as the page renders.
    /// </summary>
    internal class MappingDataWriter : IDisposable
    {
        private BinaryWriter _binaryWriter;
        private bool _wroteAnyData = false;
        
        internal MappingDataWriter(IHttpSocketAdapter mappingDataSocket)
        {
            _binaryWriter = new BinaryWriter(new HttpAdapterRequestStream(mappingDataSocket));
        }
        
        public void Dispose()
        {
            if (_binaryWriter != null)
            {
                _binaryWriter.Flush();
                _binaryWriter.Dispose();
                _binaryWriter = null;
            }
        }
        
        public void WriteBeginContext(int sourceStartPosition, int sourceLength, bool isLiteral, string sourceFilePath, int renderedOutputIndex, int renderedPosition)
        {
            WriteType(BrowserLinkConstants.MappingDataType.BeginContext);
            
            WriteValue(BrowserLinkConstants.MappingDataValue.SourceStartPosition, sourceStartPosition);
            WriteValue(BrowserLinkConstants.MappingDataValue.SourceLength, sourceLength);
            WriteValue(BrowserLinkConstants.MappingDataValue.IsLiteral, isLiteral);
            WriteValue(BrowserLinkConstants.MappingDataValue.SourceFilePath, sourceFilePath);
            WriteValue(BrowserLinkConstants.MappingDataValue.RenderedOutputIndex, renderedOutputIndex);
            WriteValue(BrowserLinkConstants.MappingDataValue.RenderedPosition, renderedPosition);
            
            WriteEndOfDataBlock();
        }
        
        public void WriteEndContext(int renderedOutputIndex, int renderedPosition)
        {
            WriteType(BrowserLinkConstants.MappingDataType.EndContext);
            
            WriteValue(BrowserLinkConstants.MappingDataValue.RenderedOutputIndex, renderedOutputIndex);
            WriteValue(BrowserLinkConstants.MappingDataValue.RenderedPosition, renderedPosition);
            
            WriteEndOfDataBlock();
        }
        
        public void WriteOutputDefinition(int renderedOutputIndex, string renderedContent)
        {
            WriteType(BrowserLinkConstants.MappingDataType.RenderedOutputDefinition);
            
            WriteValue(BrowserLinkConstants.MappingDataValue.RenderedOutputIndex, renderedOutputIndex);
            WriteValue(BrowserLinkConstants.MappingDataValue.RenderedContent, renderedContent);
            
            WriteEndOfDataBlock();
        }
        
        public void WriteTextRelationship(int parentRenderedOutputIndex, int childRenderedOutputIndex, int relativeRenderedPosition)
        {
            WriteType(BrowserLinkConstants.MappingDataType.RenderedOutputRelationship);
            
            WriteValue(BrowserLinkConstants.MappingDataValue.ParentRenderedOutputIndex, parentRenderedOutputIndex);
            WriteValue(BrowserLinkConstants.MappingDataValue.ChildRenderedOutputIndex, childRenderedOutputIndex);
            WriteValue(BrowserLinkConstants.MappingDataValue.RelativeRenderedPosition, relativeRenderedPosition);
            
            WriteEndOfDataBlock();
        }
        
        public void WriteEndOfData()
        {
            if (_wroteAnyData)
            {
                WriteType(BrowserLinkConstants.MappingDataType.EndOfData);
                WriteEndOfDataBlock();
            }
        }
        
        private void WriteType(BrowserLinkConstants.MappingDataType type)
        {
            _wroteAnyData = true;

            _binaryWriter.Write((int)type);
        }
        
        private void WriteValue(BrowserLinkConstants.MappingDataValue valueKey, int value)
        {
            _binaryWriter.Write((int)valueKey);
            _binaryWriter.Write((int)BrowserLinkConstants.MappingDataValueType.Int32Value);
            _binaryWriter.Write(value);
        }
        
        private void WriteValue(BrowserLinkConstants.MappingDataValue valueKey, string value)
        {
            _binaryWriter.Write((int)valueKey);
            _binaryWriter.Write((int)BrowserLinkConstants.MappingDataValueType.StringValue);
            _binaryWriter.Write(value);
        }
        
        private void WriteValue(BrowserLinkConstants.MappingDataValue valueKey, bool value)
        {
            _binaryWriter.Write((int)valueKey);
            _binaryWriter.Write((int)BrowserLinkConstants.MappingDataValueType.BooleanValue);
            _binaryWriter.Write(value);
        }
        
        private void WriteEndOfDataBlock()
        {
            _binaryWriter.Write((int)BrowserLinkConstants.MappingDataValue.EndOfDataValues);
        }
    }
}