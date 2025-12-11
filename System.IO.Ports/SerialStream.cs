// Copyright (c) .NET Foundation and Contributors
// See LICENSE file in the project root for full license information.

namespace System.IO.Ports
{
    internal class SerialStream : Stream
    {
        private readonly SerialPort _serial;

        internal SerialStream(SerialPort serial)
        {
            _serial = serial;
        }

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Length => throw new NotSupportedException();

        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        public override void Flush()
        {
            // writing to the stream always sends all the buffer (or times out)
            // so, there is no real use for this call
            // adding it just to follow implementation of Stream class
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return _serial.Read(buffer, offset, count);
        }

        public override int Read(Span<byte> buffer)
        {
            throw new NotImplementedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public override void Write(byte[] buffer, int offset, int count)
        {
            _serial.Write(buffer, offset, count);
        }
    }
}
