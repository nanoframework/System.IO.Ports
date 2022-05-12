//
// Copyright (c) .NET Foundation and Contributors
// See LICENSE file in the project root for full license information.
//

namespace System.IO.Ports
{
    /// <summary>
    /// Specifies the type of character that was received on the serial port of the <see cref="SerialPort"/> object.
    /// </summary>
    /// <remarks>
    /// This enumeration is used with the <see cref="SerialPort.DataReceived"/> event. 
    /// You examine the type of character that was received by retrieving the value of the <see cref="SerialDataReceivedEventArgs.EventType"/> property.
    /// The EventType property contains one of the values from the `SerialData` enumeration.
    /// </remarks>
    public enum SerialData
    {
        /// <summary>
        /// A character was received and placed in the input buffer.
        /// </summary>
        Chars = 1,

        /// <summary>
        ///  The `watch` character was received and placed in the input buffer.
        /// </summary>
        /// <remarks>
        /// This is only supported on .Net nanoFramework.
        /// </remarks>
        WatchChar = 2
    }
}
