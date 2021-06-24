using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.VisualStudio.Web.BrowserLink
{
    internal class SendFilesWrapper : IHttpResponseBodyFeature
    {
        private readonly HttpResponse _response;
        private readonly IHttpResponseBodyFeature _wrapped;

        internal SendFilesWrapper(IHttpResponseBodyFeature wrapped, HttpResponse response)
        {
            _wrapped = wrapped;
            _response = response;
        }

        public Stream Stream => _wrapped?.Stream;

        public PipeWriter Writer => _wrapped?.Writer;

        public async Task CompleteAsync()
        {
            if (_wrapped != null)
            {
                await _wrapped.CompleteAsync();
            }
        }

        public void DisableBuffering()
        {
            _wrapped?.DisableBuffering();
        }

        public async Task SendFileAsync(string path, long offset, long? count, CancellationToken cancellationToken = default)
        {
            // TODO: Send mapping data to VS

            if (_wrapped != null)
            {
                await _wrapped.SendFileAsync(path, offset, count, cancellationToken);
                return;
            }

            using (Stream readStream = File.OpenRead(path))
            {
                readStream.Seek(offset, SeekOrigin.Begin);

                await readStream.CopyToAsync(_response.Body, 4096, cancellationToken);
            }
        }

        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            if (_wrapped != null)
            {
                await _wrapped.StartAsync(cancellationToken);
            }
        }
    }
}
