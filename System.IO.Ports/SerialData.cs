﻿//
// Copyright (c) .NET Foundation and Contributors
// See LICENSE file in the project root for full license information.
//

namespace System.IO.Ports
{
    /// <summary>
    /// Specifies the type of character that was received on the serial port of the <see cref="SerialPort"/> object.
    /// </summary>
    public enum SerialData
    {
        /// <summary>
        /// A character was received and placed in the input stream.
        /// </summary>
        Chars = 1,

        /// <summary>
        ///  The character to watch was received in the input buffer and prior content placed in the input stream.
        /// </summary>
        WatchChar = 2
    }
}
