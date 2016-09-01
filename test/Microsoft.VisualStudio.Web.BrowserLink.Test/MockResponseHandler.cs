using System;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.VisualStudio.Web.BrowserLink
{
    internal class MockResponseHandler
    {
        private StringBuilder _response = new StringBuilder();
        private Encoding _encoding = Encoding.ASCII;

        private TaskCompletionSource<object> _handlerTcs = null;
        private bool _block;

        internal MockResponseHandler(Encoding encoding)
        {
            _encoding = encoding;
        }

        internal string Response
        {
            get { return _response.ToString(); }
        }

        internal bool Block
        {
            get { return _block; }
            set
            {
                _block = value;

                if (!_block && _handlerTcs != null)
                {
                    TaskCompletionSource<object> completedTcs = _handlerTcs;

                    _handlerTcs = null;

                    completedTcs.SetResult(null);
                }
            }
        }

        internal Task HandlerMethod(byte[] buffer, int index, int count)
        {
            AssertWithMessage.Null(_handlerTcs, "More than one call to HandlerMethod is happening at the same time");

            TaskCompletionSource<object> handlerTcs = new TaskCompletionSource<object>();

            try
            {
                _response.Append(_encoding.GetString(buffer, index, count));

                if (Block)
                {
                    _handlerTcs = handlerTcs;
                }
                else
                {
                    handlerTcs.SetResult(null);
                }
            }
            catch (Exception ex)
            {
                handlerTcs.SetException(ex);
            }

            return handlerTcs.Task;
        }
    }
}
