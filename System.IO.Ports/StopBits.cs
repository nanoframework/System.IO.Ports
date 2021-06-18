//
// Copyright (c) .NET Foundation and Contributors
// See LICENSE file in the project root for full license information.
//

namespace System.IO.Ports
{
    /// <summary>
    /// Specifies the number of stop bits used on the <see cref="SerialPort"/> object.
    /// </summary>
    public enum StopBits
    {        
        /// <summary>
        ///  One stop bit is used.
        /// </summary>
        One = 1,

        /// <summary>
        /// Two stop bits are used.
        /// </summary>
        Two = 2,

        /// <summary>
        /// 1.5 stop bits are used.
        /// </summary>
        OnePointFive = 3
    };
}