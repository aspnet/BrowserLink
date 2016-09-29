using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.VisualStudio.Web.BrowserLink.Runtime
{
    internal class SendFilesWrapper : IHttpSendFileFeature
    {
        private HttpResponse _response;
        private IHttpSendFileFeature _wrapped;

        internal SendFilesWrapper(IHttpSendFileFeature wrapped, HttpResponse response)
        {
            _wrapped = wrapped;
            _response = response;
        }

        async Task IHttpSendFileFeature.SendFileAsync(string path, long offset, long? count, CancellationToken cancellation)
        {
            // TODO: Send mapping data to VS

            if (_wrapped != null)
            {
                await _wrapped.SendFileAsync(path, offset, count, cancellation);
                return;
            }

            using (Stream readStream = File.OpenRead(path))
            {
                readStream.Seek(offset, SeekOrigin.Begin);

                await readStream.CopyToAsync(_response.Body, 4096, cancellation);
            }
        }
    }
}
