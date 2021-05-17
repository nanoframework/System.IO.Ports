namespace System.IO.Ports
{
    /// <summary>
    /// Represents the method that will handle the System.IO.Ports.SerialPort.DataReceived
    /// event of a System.IO.Ports.SerialPort object.
    /// </summary>
    /// <param name="sender">The sender of the event, which is the System.IO.Ports.SerialPort object.</param>
    /// <param name="e">A System.IO.Ports.SerialDataReceivedEventArgs object that contains the event data.</param>
    public delegate void SerialDataReceivedEventHandler(object sender, SerialDataReceivedEventArgs e);
}