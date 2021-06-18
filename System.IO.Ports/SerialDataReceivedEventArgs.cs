//
// Copyright (c) .NET Foundation and Contributors
// See LICENSE file in the project root for full license information.
//

namespace System.IO.Ports
{
    /// <summary>
    /// Provides data for the <see cref="SerialPort.DataReceived"/> event.
    /// </summary>
    public class SerialDataReceivedEventArgs : EventArgs
    {
        private readonly SerialData _data;

        internal SerialDataReceivedEventArgs(SerialData eventCode)
        {
            _data = eventCode;
        }

        /// <summary>
        /// Gets the event type.
        /// </summary>
        public SerialData EventType { get => _data; }
    }
}