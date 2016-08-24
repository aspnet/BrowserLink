using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.VisualStudio.Web.BrowserLink
{
    internal class MockSocketAdapter : ISocketAdapter
    {
        private TaskCompletionSource<int> _receiveTcs = null;

        private ArraySegment<byte>? _currentBytesToSend = null;
        private ArraySegment<byte> _receiverBuffer = new ArraySegment<byte>();
        private Queue<ArraySegment<byte>> _bytesToSend = new Queue<ArraySegment<byte>>();

        private StringBuilder _sentContent = new StringBuilder();

        private bool _disposed = false;
        private bool _throwExceptionOnNextSendAsync = false;

        public string SentContent
        {
            get { return _sentContent.ToString(); }
        }

        private ArraySegment<byte>? CurrentBytesToSend
        {
            get
            {
                if (_currentBytesToSend == null && _bytesToSend.Count > 0)
                {
                    _currentBytesToSend = _bytesToSend.Dequeue();
                }

                return _currentBytesToSend;
            }
        }

        public bool IsClosed
        {
            get { return _disposed; }
        }

        internal void ThrowExceptionOnNextSendAsync()
        {
            _throwExceptionOnNextSendAsync = true;
        }

        internal void ThrowExceptionFromReceiveAsync(string message = "An error occurred.")
        {
            if (_receiveTcs != null)
            {
                _receiveTcs.TrySetException(new Exception(message));
                _receiveTcs = null;
            }
        }

        public void SendString(string data, Encoding encoding)
        {
            byte[] bytes = encoding.GetBytes(data);

            SendBytes(bytes, 0, bytes.Length);
        }

        public void SendBytes(byte[] buffer, int offset, int count)
        {
            _bytesToSend.Enqueue(new ArraySegment<byte>(buffer, offset, count));

            PushBytesToReceiver();
        }

        private void PushBytesToReceiver()
        {
            if (_receiveTcs != null && CurrentBytesToSend != null)
            {
                int bytesLeftInReceiverBuffer = _receiverBuffer.Count;
                int positionInReceiverBuffer = _receiverBuffer.Offset;
                int bytesSent = 0;

                while (bytesLeftInReceiverBuffer > 0 && CurrentBytesToSend != null)
                {
                    int bytesToSend = Math.Min(bytesLeftInReceiverBuffer, CurrentBytesToSend.Value.Count);

                    Array.Copy(CurrentBytesToSend.Value.Array, CurrentBytesToSend.Value.Offset, _receiverBuffer.Array, positionInReceiverBuffer, bytesToSend);

                    if (bytesToSend == CurrentBytesToSend.Value.Count)
                    {
                        _currentBytesToSend = null;
                    }
                    else
                    {
                        _currentBytesToSend = new ArraySegment<byte>(
                            CurrentBytesToSend.Value.Array,
                            CurrentBytesToSend.Value.Offset + bytesToSend,
                            CurrentBytesToSend.Value.Count - bytesToSend);
                    }

                    bytesSent += bytesToSend;

                    if (bytesToSend == bytesLeftInReceiverBuffer)
                    {
                        break;
                    }
                    else
                    {
                        bytesLeftInReceiverBuffer -= bytesToSend;
                        positionInReceiverBuffer += bytesToSend;
                    }
                }

                TaskCompletionSource<int> completedTcs = _receiveTcs;
                _receiveTcs = null;

                completedTcs.SetResult(bytesSent);
            }
        }

        Task<int> ISocketAdapter.ReceiveAsync(byte[] buffer, int offset, int count)
        {
            Assert.False(_disposed, "ReceiveAsync was called on a socket that has been disposed.");

            AssertWithMessage.Null(_receiveTcs, "Multiple calls to ReceiveAsync are occuring at the same time");

            _receiveTcs = new TaskCompletionSource<int>();
            _receiverBuffer = new ArraySegment<byte>(buffer, offset, count);

            Task<int> resultTask = _receiveTcs.Task;

            PushBytesToReceiver();

            return resultTask;
        }

        Task ISocketAdapter.SendAsync(IList<ArraySegment<byte>> buffers)
        {
            Assert.False(_disposed, "SendAsync was called on a socket that has been disposed.");

            TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();

            try
            {
                if (_throwExceptionOnNextSendAsync)
                {
                    tcs.SetException(new Exception("SendAsync after ThrowExceptionOnNextSendAsync was called."));
                }
                else
                {
                    foreach (ArraySegment<byte> buffer in buffers)
                    {
                        _sentContent.Append(Encoding.ASCII.GetString(buffer.Array, buffer.Offset, buffer.Count));
                    }

                    tcs.SetResult(null);
                }
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }

            return tcs.Task;
        }

        void IDisposable.Dispose()
        {
            _disposed = true;
        }
    }

}
