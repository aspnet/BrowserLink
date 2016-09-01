// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Web.BrowserLink
{
    /// <summary>
    /// A wrapper for a TextWriter that allows us to record mapping information
    /// as content is written to the output.
    /// </summary>
    internal class TextWriterDecorator : TextWriter
    {
        private TextWriter _decoratedWriter;
        private StringWriter _bufferWriter;
        private PageExecutionListenerFeature _listener;

        internal TextWriterDecorator(TextWriter decoratedWriter, PageExecutionListenerFeature listener, int renderdOutputIndex)
        {
            _decoratedWriter = decoratedWriter;
            _listener = listener;

            _bufferWriter = new StringWriter(_decoratedWriter.FormatProvider);
            _bufferWriter.NewLine = _decoratedWriter.NewLine;

            RenderedOutputIndex = renderdOutputIndex;
        }

        /// <summary>
        /// Returns the current character position where output is being written.
        /// </summary>
        internal int OutputPosition
        {
            get { return _bufferWriter.GetStringBuilder().Length; }
        }

        /// <summary>
        /// An ID used to identify this writer withing the current page request.
        /// </summary>
        internal int RenderedOutputIndex
        {
            get;
            private set;
        }

        /// <summary>
        /// The complete content that has been written to this writer.
        /// </summary>
        internal string RenderedOutput
        {
            get { return _bufferWriter.GetStringBuilder().ToString(); }
        }

        #region TextWriter implementation
        // All TextWriter method implementations need to, at a minimum, pass exactly
        // the same method call to the decorated writer. Then they can do optional
        // recording operations. Usually this means writing the same data to a
        // second writer to remember what was written.

        public override Encoding Encoding
        {
            get { return _decoratedWriter.Encoding; }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _decoratedWriter.Dispose();

                _bufferWriter.Dispose();
            }
        }

        public override void Flush()
        {
            _decoratedWriter.Flush();
        }

        public override Task FlushAsync()
        {
            return _decoratedWriter.FlushAsync();
        }

        public override IFormatProvider FormatProvider
        {
            get { return _decoratedWriter.FormatProvider; }
        }

        public override string NewLine
        {
            get { return _decoratedWriter.NewLine; }

            set
            {
                _decoratedWriter.NewLine = value;

                _bufferWriter.NewLine = value;
            }
        }

        public override void Write(char value)
        {
            _decoratedWriter.Write(value);

            _bufferWriter.Write(value);
        }

        public override void Write(bool value)
        {
            _bufferWriter.Write(value);

            _decoratedWriter.Write(value);
        }

        public override void Write(char[] buffer)
        {
            _bufferWriter.Write(buffer);

            _decoratedWriter.Write(buffer);
        }

        public override void Write(char[] buffer, int index, int count)
        {
            _bufferWriter.Write(buffer, index, count);

            _decoratedWriter.Write(buffer, index, count);
        }

        public override void Write(decimal value)
        {
            _bufferWriter.Write(value);

            _decoratedWriter.Write(value);
        }

        public override void Write(double value)
        {
            _bufferWriter.Write(value);

            _decoratedWriter.Write(value);
        }

        public override void Write(float value)
        {
            _bufferWriter.Write(value);

            _decoratedWriter.Write(value);
        }

        public override void Write(int value)
        {
            _bufferWriter.Write(value);

            _decoratedWriter.Write(value);
        }

        public override void Write(long value)
        {
            _bufferWriter.Write(value);

            _decoratedWriter.Write(value);
        }

        public override void Write(object value)
        {
            _bufferWriter.Write(value);

            _decoratedWriter.Write(value);
        }

        public override void Write(string value)
        {
            _bufferWriter.Write(value);

            _decoratedWriter.Write(value);
        }

        public override void Write(uint value)
        {
            _bufferWriter.Write(value);

            _decoratedWriter.Write(value);
        }

        public override void Write(ulong value)
        {
            _bufferWriter.Write(value);

            _decoratedWriter.Write(value);
        }

        public override Task WriteAsync(char value)
        {
            _bufferWriter.Write(value);

            return _decoratedWriter.WriteAsync(value);
        }

        public override Task WriteAsync(char[] buffer, int index, int count)
        {
            _bufferWriter.Write(buffer, index, count);

            return _decoratedWriter.WriteAsync(buffer, index, count);
        }

        public override Task WriteAsync(string value)
        {
            _bufferWriter.Write(value);

            return _decoratedWriter.WriteAsync(value);
        }

        public override void WriteLine()
        {
            _bufferWriter.WriteLine();

            _decoratedWriter.WriteLine();
        }

        public override void WriteLine(bool value)
        {
            _bufferWriter.WriteLine(value);

            _decoratedWriter.WriteLine(value);
        }

        public override void WriteLine(char value)
        {
            _bufferWriter.WriteLine(value);

            _decoratedWriter.WriteLine(value);
        }

        public override void WriteLine(char[] buffer)
        {
            _bufferWriter.WriteLine(buffer);

            _decoratedWriter.WriteLine(buffer);
        }

        public override void WriteLine(char[] buffer, int index, int count)
        {
            _bufferWriter.WriteLine(buffer, index, count);

            _decoratedWriter.WriteLine(buffer, index, count);
        }

        public override void WriteLine(decimal value)
        {
            _bufferWriter.WriteLine(value);

            _decoratedWriter.WriteLine(value);
        }

        public override void WriteLine(double value)
        {
            _bufferWriter.WriteLine(value);

            _decoratedWriter.WriteLine(value);
        }

        public override void WriteLine(float value)
        {
            _bufferWriter.WriteLine(value);

            _decoratedWriter.WriteLine(value);
        }

        public override void WriteLine(int value)
        {
            _bufferWriter.WriteLine(value);

            _decoratedWriter.WriteLine(value);
        }

        public override void WriteLine(long value)
        {
            _bufferWriter.WriteLine(value);

            _decoratedWriter.WriteLine(value);
        }

        public override void WriteLine(object value)
        {
            _bufferWriter.WriteLine(value);

            _decoratedWriter.WriteLine(value);
        }

        public override void WriteLine(string value)
        {
            _bufferWriter.WriteLine(value);

            _decoratedWriter.WriteLine(value);
        }

        public override void WriteLine(uint value)
        {
            _bufferWriter.WriteLine(value);

            _decoratedWriter.WriteLine(value);
        }

        public override void WriteLine(ulong value)
        {
            _bufferWriter.WriteLine(value);

            _decoratedWriter.WriteLine(value);
        }

        public override Task WriteLineAsync()
        {
            _bufferWriter.WriteLine();

            return _decoratedWriter.WriteLineAsync();
        }

        public override Task WriteLineAsync(char value)
        {
            _bufferWriter.WriteLine(value);

            return _decoratedWriter.WriteLineAsync(value);
        }

        public override Task WriteLineAsync(char[] buffer, int index, int count)
        {
            _bufferWriter.WriteLine(buffer, index, count);

            return _decoratedWriter.WriteLineAsync(buffer, index, count);
        }

        public override Task WriteLineAsync(string value)
        {
            _bufferWriter.WriteLine(value);

            return _decoratedWriter.WriteLineAsync(value);
        }
        #endregion
    }
}