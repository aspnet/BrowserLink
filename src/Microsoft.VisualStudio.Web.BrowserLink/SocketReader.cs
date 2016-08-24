// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Web.BrowserLink
{
    /// <summary>
    /// This is a helper class for reading data from a Socket. It encapsulates
    /// the ability to alternately read decoded characters and lines, or raw
    /// data, from the same data source.
    /// </summary>
    internal class SocketReader
    {
        private ISocketAdapter _socket;
        private DecoderAdapter _decoderAdapter = new DecoderAdapter();

        private byte[] _buffer = new byte[10240];
        private int _bufferCurrentPosition = 0;
        private int _bufferStopPosition = 0;

        internal SocketReader(ISocketAdapter socket)
        {
            _socket = socket;
        }

        /// <summary>
        /// Sets the Encoding that will be used for ReadChar and ReadLine.
        /// </summary>
        public void SetEncoding(Encoding encoding)
        {
            _decoderAdapter.SetDecoder(encoding.GetDecoder());
        }

        /// <summary>
        /// Read characters from the socket until a CRLF is encountered.
        /// </summary>
        /// <returns>
        /// A task that completes when enough data has been returned by the server.
        /// The task returns the text of the line, excluding the CRLF.
        /// </returns>
        public async Task<string> ReadLine(CancellationToken cancellationToken)
        {
            StringBuilder line = new StringBuilder();

            bool foundCr = false;

            while (true)
            {
                char character = await ReadChar(cancellationToken);

                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                if (character == '\r')
                {
                    if (!foundCr)
                    {
                        foundCr = true;

                        continue;
                    }
                }
                else if (foundCr)
                {
                    if (character == '\n')
                    {
                        break;
                    }
                    else
                    {
                        foundCr = false;

                        line.Append('\r');
                    }
                }

                line.Append(character);
            }

            return line.ToString();
        }

        /// <summary>
        /// Read a single character from the socket.
        /// </summary>
        /// <returns>A task that completes when enough data has been returned from the server.</returns>
        public async Task<char> ReadChar(CancellationToken cancellationToken)
        {
            char result;

            while (!DecodeChar(out result))
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                await ReceiveMoreBytes(cancellationToken);
            }

            return result;
        }

        /// <summary>
        /// Reads some raw data into a ResponseHandler method.
        /// </summary>
        /// <returns>
        /// A task that completes when enough data has been returned by the server.
        /// The task returns the number of bytes sent to the ResponseHandler.
        /// </returns>
        public async Task<int> ReadBytesIntoResponseHandler(long totalBytesToRead, ResponseHandler handler, CancellationToken cancellationToken)
        {
            long totalBytesRead = 0;

            while (totalBytesToRead > 0)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                int bytesRemainingInBuffer = _bufferStopPosition - _bufferCurrentPosition;

                if (bytesRemainingInBuffer == 0)
                {
                    await ReceiveMoreBytes(cancellationToken);

                    bytesRemainingInBuffer = _bufferStopPosition - _bufferCurrentPosition;
                }

                int bytesToRead;
                if (totalBytesToRead < bytesRemainingInBuffer)
                {
                    // This cast is safe because totalBytesToRead < bytesRemainingInBuffer,
                    // and bytesRemainingInBuffer is an int.
                    bytesToRead = (int)totalBytesToRead;
                }
                else
                {
                    bytesToRead = bytesRemainingInBuffer;
                }

                await handler.Invoke(_buffer, _bufferCurrentPosition, bytesToRead);

                _bufferCurrentPosition += bytesToRead;
                totalBytesRead += bytesToRead;
                totalBytesToRead -= bytesToRead;
            }

            return (int)totalBytesRead;
        }

        private async Task<bool> ReceiveMoreBytes(CancellationToken cancellationToken)
        {
            int bytesRemainingInBuffer = _bufferStopPosition - _bufferCurrentPosition;
            int bytesReceived = 0;

            if (bytesRemainingInBuffer > 0)
            {
                Array.Copy(_buffer, _bufferCurrentPosition, _buffer, 0, bytesRemainingInBuffer);

                _bufferCurrentPosition = 0;
                _bufferStopPosition = bytesRemainingInBuffer;
            }

            int spaceInBuffer = _buffer.Length - bytesRemainingInBuffer;

            if (spaceInBuffer > 0)
            {
                bytesReceived = await TaskHelpers.WaitWithCancellation(
                    _socket.ReceiveAsync(_buffer, bytesRemainingInBuffer, spaceInBuffer),
                    cancellationToken, 0);

                _bufferCurrentPosition = 0;
                _bufferStopPosition = bytesRemainingInBuffer + bytesReceived;
            }

            return bytesReceived > 0;
        }

        private bool DecodeChar(out char result)
        {
            return _decoderAdapter.DecodeCharacter(_buffer, ref _bufferCurrentPosition, _bufferStopPosition - _bufferCurrentPosition, out result);
        }

        /// <summary>
        /// This helper class maintains state used in decoding characters
        /// that may be multi-byte, when all the bytes might not be returned
        /// by the server at the same time (or they might fall on the edge of
        /// the buffer used to read data from the server).
        /// </summary>
        private class DecoderAdapter
        {
            private Decoder _decoder = Encoding.ASCII.GetDecoder();

            private char[] characterBuffer = new char[1];

            public void SetDecoder(Decoder decoder)
            {
                _decoder = decoder;
            }

            public bool DecodeCharacter(byte[] buffer, ref int bufferPosition, int bufferBytes, out char result)
            {
                if (bufferBytes >= 1)
                {
                    int bytesToUse = 1;
                    int charactersReturned = 0;

                    while (bytesToUse < bufferBytes)
                    {
                        charactersReturned = _decoder.GetCharCount(buffer, bufferPosition, bytesToUse);

                        if (charactersReturned == 1)
                        {
                            break;
                        }

                        bytesToUse++;
                    }

                    charactersReturned = _decoder.GetChars(buffer, bufferPosition, bytesToUse, characterBuffer, 0);

                    bufferPosition += bytesToUse;

                    if (charactersReturned == 1)
                    {
                        result = characterBuffer[0];
                        return true;
                    }
                }

                result = '\0';
                return false;
            }
        }
    }
}