// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Web.BrowserLink
{
    /// <summary>
    /// A first-in-first-out data buffering system that reuses existing buffers
    /// to minimize the amount of memory required.
    /// </summary>
    /// <typeparam name="DataType">The type of data being stored</typeparam>
    internal sealed class RevolvingBuffers<DataType> : IDisposable
    {
        private int _defaultBufferSize;
        private object _lockObject = new object();

        private LinkedList<Buffer> _buffers = new LinkedList<Buffer>();
        private LinkedListNode<Buffer> _currentInputBuffer;
        private LinkedListNode<Buffer> _currentOutputBuffer;

        private TaskCompletionSource<ArraySegment<DataType>> _getBufferedData = null;
        private TaskCompletionSource<bool> _waitForBufferEmpty = null;

        /// <summary>
        /// Initialize the RevolvingBuffers
        /// </summary>
        /// <param name="defaultBufferSize">
        /// The default size of an individual buffer. Larger buffers may be created
        /// to accomodate larger sets of input data.
        /// </param>
        internal RevolvingBuffers(int defaultBufferSize)
        {
            _defaultBufferSize = defaultBufferSize;

            _currentInputBuffer = _buffers.AddFirst(new Buffer(defaultBufferSize));
        }

        private Buffer InputBuffer
        {
            get { return _currentInputBuffer.Value; }
        }

        private Buffer OutputBuffer
        {
            get { return _currentOutputBuffer.Value; }
        }

        private bool HasAsyncReader
        {
            get { return _getBufferedData != null; }
        }

#if DEBUG
        // FOR UNIT TESTING ONLY
        internal int BufferCount
        {
            get { return _buffers.Count; }
        }
#endif

        /// <summary>
        /// Clear and free all the buffers. This also alerts asynchronous readers
        /// that no more data is coming.
        /// </summary>
        public void Dispose()
        {
            lock (_lockObject)
            {
                GiveBufferToAsyncReader(default(ArraySegment<DataType>));

                _buffers.Clear();
                _currentOutputBuffer = null;
                _currentInputBuffer = null;
            }
        }

        /// <summary>
        /// Copy data into the buffers.
        /// </summary>
        /// <param name="arraySegment">Segment of an array to copy</param>
        public void CopyDataToBuffer(ArraySegment<DataType> arraySegment)
        {
            CopyDataToBuffer(arraySegment.Array, arraySegment.Offset, arraySegment.Count);
        }

        /// <summary>
        /// Copy data into the buffers.
        /// </summary>
        /// <param name="data">An array containing the data to copy</param>
        /// <param name="offset">The starting index of the data to copy</param>
        /// <param name="count">The number of data elements to copy</param>
        public void CopyDataToBuffer(DataType[] data, int offset, int count)
        {
            lock (_lockObject)
            {
                EnsureSpaceInInputBuffer(count);

                CopyDataToInputBuffer(data, offset, count);

                if (HasAsyncReader && AdvanceToNextOutputBuffer())
                {
                    GiveBufferToAsyncReader(OutputBuffer.CreateArraySegment());
                }
            }
        }

        /// <summary>
        /// Synchronously reads data from the buffer.
        /// </summary>
        /// <returns>
        /// An ArraySegment containing buffered data, or an empty buffer if there is no
        /// data to read.
        /// </returns>
        /// <remarks>
        /// The ArraySegment remains valid until GetBufferedData/Async is called again.
        /// After the next call to this method, the buffer in the ArraySegment can be
        /// reused for future data.
        /// </remarks>
        public ArraySegment<DataType> GetBufferedData()
        {
            lock (_lockObject)
            {
                if (AdvanceToNextOutputBuffer())
                {
                    return OutputBuffer.CreateArraySegment();
                }
                else
                {
                    SignalBufferEmpty();

                    return default(ArraySegment<DataType>);
                }
            }
        }

        /// <summary>
        /// Asynchronously reads data from the buffer.
        /// </summary>
        /// <returns>
        /// A task that returns an ArraySegment containing buffered data. If there
        /// is no buffered data, the task does not complete until more data is
        /// buffered, or the buffers are disposed.
        /// </returns>
        /// <remarks>
        /// The ArraySegment remains valid until GetBufferedData/Async is called again.
        /// After the next call to this method, the buffer in the ArraySegment can be
        /// reused for future data.
        /// </remarks>
        public Task<ArraySegment<DataType>> GetBufferedDataAsync()
        {
            lock (_lockObject)
            {
                if (AdvanceToNextOutputBuffer())
                {
                    return Task.FromResult(OutputBuffer.CreateArraySegment());
                }
                else
                {
                    _getBufferedData = new TaskCompletionSource<ArraySegment<DataType>>();

                    // We need a local reference to the task here, because SignalBufferEmpty()
                    // could end up calling Dispose(), and that will complete and then clear
                    // _getBufferedData. We still want to return the completed task to the caller
                    // of this method.
                    Task<ArraySegment<DataType>> task = _getBufferedData.Task;

                    SignalBufferEmpty();

                    return task;
                }
            }
        }

        /// <summary>
        /// Wait until all data has been read from the buffer.
        /// </summary>
        /// <returns>A task that completes when the last data has been read.</returns>
        public Task WaitForBufferEmptyAsync()
        {
            lock (_lockObject)
            {
                if (_buffers.First.Value.DataCount == 0)
                {
                    return StaticTaskResult.True;
                }

                _waitForBufferEmpty = new TaskCompletionSource<bool>();

                return _waitForBufferEmpty.Task;
            }
        }

        private void CopyDataToInputBuffer(DataType[] data, int offset, int count)
        {
            Array.Copy(data, offset, InputBuffer.Data, InputBuffer.DataCount, count);

            InputBuffer.DataCount += count;
        }

        private void EnsureSpaceInInputBuffer(int requiredCount)
        {
            if (InputBuffer.AvailableCount < requiredCount)
            {
                AdvanceToNextInputBuffer(requiredCount);
            }
        }

        private bool AdvanceToNextInputBuffer(int requiredCount = 0)
        {
            LinkedListNode<Buffer> nextEmptyBuffer = GetNextEmptyInputBuffer(_currentInputBuffer);

            if (nextEmptyBuffer == null)
            {
                nextEmptyBuffer = _buffers.AddLast(CreateNewBuffer(requiredCount));
            }
            else if (nextEmptyBuffer.Value.AvailableCount < requiredCount)
            {
                nextEmptyBuffer = _buffers.AddBefore(nextEmptyBuffer, CreateNewBuffer(requiredCount));
            }

            if (_currentInputBuffer != nextEmptyBuffer)
            {
                _currentInputBuffer = nextEmptyBuffer;

                // Buffer was advanced
                return true;
            }
            else
            {
                // Buffer did not change
                return false;
            }
        }

        private LinkedListNode<Buffer> GetNextEmptyInputBuffer(LinkedListNode<Buffer> currentBuffer)
        {
            if (!currentBuffer.Value.HasAnyData)
            {
                return currentBuffer;
            }
            else
            {
                return currentBuffer.Next;
            }
        }

        private Buffer CreateNewBuffer(int requiredCount)
        {
            int bufferSize = _defaultBufferSize;

            while (bufferSize < requiredCount)
            {
                bufferSize = bufferSize * 2;
            }

            return new Buffer(bufferSize);
        }

        private bool AdvanceToNextOutputBuffer()
        {
            RecycleCurrentOutputBuffer();

            // Output buffer is always first in the list
            _currentOutputBuffer = _buffers.First;

            if (_currentOutputBuffer == _currentInputBuffer)
            {
                if (!AdvanceToNextInputBuffer())
                {
                    _currentOutputBuffer = null;
                }
            }

            return _currentOutputBuffer != null;
        }

        private void RecycleCurrentOutputBuffer()
        {
            if (_currentOutputBuffer != null)
            {
                _currentOutputBuffer.Value.DataCount = 0;

                // The buffer is moved to the end of the list, putting it in line
                // as a potential input buffer
                _buffers.AddLast(_currentOutputBuffer.Value);
                _buffers.Remove(_currentOutputBuffer);
            }
        }
        
        private void GiveBufferToAsyncReader(ArraySegment<DataType> buffer)
        {
            if (_getBufferedData != null)
            {
                // _getBufferedData must be set to null before calling TrySetResult. The
                // handler will likely try to read more data immediately, which will set
                // a new _getBufferedData. If we clear _getBufferedData after TrySetResult,
                // that will erase the new _getBufferedData instead of the old one.
                TaskCompletionSource<ArraySegment<DataType>> tcs = _getBufferedData;
                _getBufferedData = null;

                tcs.TrySetResult(buffer);
            }
        }

        private void SignalBufferEmpty()
        {
            if (_waitForBufferEmpty != null)
            {
                // _waitForBufferEmpty must be set to null before calling TrySetResult. It
                // is unlikely that the handler would call WaitForBufferEmpty again, but if
                // they do, and we clear _waitForBufferEmpty after TrySetResult, that will
                // erase the new _waitForBufferEmpty instead of the old one.
                TaskCompletionSource<bool> tcs = _waitForBufferEmpty;
                _waitForBufferEmpty = null;

                tcs.TrySetResult(true);
            }
        }

        private class Buffer
        {
            internal Buffer(int size)
            {
                Data = new DataType[size];
            }

            public DataType[] Data
            {
                get;
                private set;
            }

            public int DataCount
            {
                get;
                set;
            }

            public int AvailableCount
            {
                get { return Data.Length - DataCount; }
            }

            public bool HasAnyData
            {
                get { return DataCount > 0; }
            }

            public ArraySegment<DataType> CreateArraySegment()
            {
                return new ArraySegment<DataType>(Data, 0, DataCount);
            }
        }
    }
}
