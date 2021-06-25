//
// Copyright (c) .NET Foundation and Contributors
// See LICENSE file in the project root for full license information.
//

namespace System.IO.Ports
{
    /// <summary>
    /// Represents the method that will handle the System.IO.Ports.SerialPort.DataReceived
    /// event of a <see cref="SerialPort"/> object.
    /// </summary>
    /// <param name="sender">The sender of the event, which is the <see cref="SerialPort"/> object.</param>
    /// <param name="e">A <see cref="SerialDataReceivedEventArgs"/> object that contains the event data.</param>
    public delegate void SerialDataReceivedEventHandler(
        object sender,
        SerialDataReceivedEventArgs e);
}