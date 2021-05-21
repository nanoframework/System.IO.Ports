//
// Copyright (c) .NET Foundation and Contributors
// See LICENSE file in the project root for full license information.
//

using System;
using System.Text;

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
            _serial.NativeStore();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return (int)_serial.NativeRead(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _serial.Write(buffer, offset, count);
        }
    }
}
