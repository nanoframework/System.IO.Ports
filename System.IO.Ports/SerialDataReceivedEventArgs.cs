namespace System.IO.Ports
{ 
    /// <summary>
    /// Provides data for the System.IO.Ports.SerialPort.DataReceived event.
    /// </summary>
    public class SerialDataReceivedEventArgs : EventArgs
    {
        SerialData _data;

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