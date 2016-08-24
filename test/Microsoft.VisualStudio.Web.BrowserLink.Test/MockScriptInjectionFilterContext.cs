using System.IO;
using System.Text;

namespace Microsoft.VisualStudio.Web.BrowserLink
{
    public class MockScriptInjectionFilterContext : IScriptInjectionFilterContext
    {
        private MemoryStream _responseBody = new MemoryStream();
        private string _requestPath;
        private string _contentType;

        public MockScriptInjectionFilterContext(string requestPath = "http://localhost:2468/Default.html", string contentType = "text/html")
        {
            _requestPath = requestPath;
            _contentType = contentType;
        }

        public string GetResponseBody(Encoding encoding)
        {
            long originalPosition = _responseBody.Position;

            try
            {
                _responseBody.Seek(0, SeekOrigin.Begin);

                byte[] buffer = new byte[_responseBody.Length];
                _responseBody.Read(buffer, 0, buffer.Length);

                return encoding.GetString(buffer, 0, buffer.Length);
            }
            finally
            {
                _responseBody.Position = originalPosition;
            }
        }

        string IScriptInjectionFilterContext.RequestPath
        {
            get { return _requestPath; }
        }

        Stream IScriptInjectionFilterContext.ResponseBody
        {
            get { return _responseBody; }
        }

        string IScriptInjectionFilterContext.ResponseContentType
        {
            get { return _contentType; }
        }
    }
}
