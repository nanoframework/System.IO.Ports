//
// Copyright (c) .NET Foundation and Contributors
// See LICENSE file in the project root for full license information.
//

using System.Runtime.CompilerServices;
using System.Text;
using System.Collections;

namespace System.IO.Ports
{
    /// <summary>
    /// Represents a serial port resource.
    /// </summary>
    public class SerialPort : IDisposable
    {
        /// <summary>
        /// Indicates that no time-out should occur.
        /// </summary>
        public const int InfiniteTimeout = -1;

        /// <summary>
        /// Default new Line, you can change and adjust it in the NewLine property
        /// </summary>
        public const string DefaultNewLine = "\r\n";

        private static SerialDeviceEventListener s_eventListener = new SerialDeviceEventListener();

        private int _writeTimeout = InfiniteTimeout;
        private int _readTimeout = InfiniteTimeout;
        private int _receivedBytesThreshold;
        private int _baudRate = 9600;
        private bool _opened = false;
        private bool _disposed = true;
        private Handshake _handshake = Handshake.None;
        private StopBits _stopBits = StopBits.One;
        private int _dataBits = 8;
        private Parity _parity = Parity.None;
        private SerialMode _mode = SerialMode.Normal;
        private string _deviceId;
        internal int _portIndex;
        private char _watchChar;
        private SerialDataReceivedEventHandler _callbacksDataReceivedEvent = null;
        private SerialStream _stream;
        private readonly object _syncLock;
        private string _newLine;
        private bool _hasBeenOpened = false;

        /// <summary>
        /// Initializes a new instance of the System.IO.Ports.SerialPort class using the
        /// specified port name, baud rate, parity bit, data bits, and stop bit.
        /// </summary>
        /// <param name="portName">The port to use (for example, COM1).</param>
        /// <param name="baudRate">The baud rate.</param>
        /// <param name="parity">One of the System.IO.Ports.SerialPort.Parity values.</param>
        /// <param name="dataBits">The data bits value.</param>
        /// <param name="stopBits">One of the System.IO.Ports.SerialPort.StopBits values.</param>
        /// <exception cref="System.IO.IOException">The specified port could not be found or opened.</exception>
        public SerialPort(string portName, int baudRate = 9600, Parity parity = Parity.None, int dataBits = 8, StopBits stopBits = StopBits.One)
        {
            _syncLock = new object();
            // the UART name is an ASCII string with the COM port name in format 'COMn'
            // need to grab 'n' from the string and convert that to the integer value from the ASCII code (do this by subtracting 48 from the char value)
            _portIndex = (portName[3] - 48);

            var device = FindDevice(_portIndex);
            if (device == null)
            {
                _deviceId = portName;
                _baudRate = baudRate;
                _parity = parity;
                _dataBits = dataBits;
                _stopBits = stopBits;
            }
            else
            {
                // this device already exists throw an exception
                throw new ArgumentException();
            }

            _newLine = DefaultNewLine;
        }

        /// <summary>
        /// Opens a new serial port connection.
        /// </summary>
        /// <exception cref="System.InvalidOperationException">The specified port on the current instance of the System.IO.Ports.SerialPort
        /// is already open.</exception>
        public void Open()
        {
            if (!_opened)
            {
                _disposed = false;
                // Initi should happen only once
                if (!_hasBeenOpened)
                {
                    NativeInit();
                }

                _hasBeenOpened = true;

                // add the serial device to the event listener in order to receive the callbacks from the native interrupts
                s_eventListener.AddSerialDevice(this);
                // add serial device to collection
                SerialDeviceController.DeviceCollection.Add(this);
                _stream = new SerialStream(this);
                _opened = true;
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        /// <summary>
        /// Closes the port connection, sets the System.IO.Ports.SerialPort.IsOpen property
        /// to false, and disposes of the internal System.IO.Stream object.
        /// </summary>
        /// <exception cref="IOException">The port is in an invalid state. -or- An attempt to set the state of the underlying
        /// port failed. For example, the parameters passed from this System.IO.Ports.SerialPort
        /// object were invalid.</exception>
        public void Close()
        {
            if (_opened)
            {
                _stream.Flush();
                // remove the pin from the event listener
                s_eventListener.RemoveSerialDevice(_portIndex);
                // find device
                var device = FindDevice(_portIndex);

                if (device != null)
                {
                    // remove device from collection
                    SerialDeviceController.DeviceCollection.Remove(device);
                }

                _stream.Dispose();
            }

            _opened = false;
        }

        /// <summary>
        /// Gets an array of serial port names for the current computer.
        /// </summary>
        /// <returns>An array of serial port names for the current computer.</returns>
        public static string[] GetPortNames()
        {
            return GetDeviceSelector().Split(',');
        }

        #region Properties

        /// <summary>
        /// Gets or sets the serial baud rate.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">The baud rate specified is less than or equal to zero, or is greater than the
        /// maximum allowable baud rate for the device.</exception>
        /// <exception cref="IOException">The port is in an invalid state. -or- An attempt to set the state of the underlying
        /// port failed. For example, the parameters passed from this System.IO.Ports.SerialPort
        /// object were invalid.</exception>
        public int BaudRate
        {
            get => _baudRate;
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException();
                }

                _baudRate = value;
                NativeConfig();
            }
        }

        //
        /// <summary>
        /// Gets or sets the handshaking protocol for serial port transmission of data using
        /// a value from System.IO.Ports.Handshake.
        /// </summary>
        /// <exception cref="System.IO.IOException">
        /// The port is in an invalid state. -or- An attempt to set the state of the underlying
        /// port failed. For example, the parameters passed from this System.IO.Ports.SerialPort
        /// object were invalid.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">The value passed is not a valid value in the System.IO.Ports.Handshake enumeration.</exception>
        public Handshake Handshake
        {
            get => _handshake;
            set
            {
                _handshake = value;
                NativeConfig();
            }
        }

        /// <summary>
        /// Gets or sets the standard length of data bits per byte.
        /// </summary>
        /// <exception cref="IOException">
        /// The port is in an invalid state. -or- An attempt to set the state of the underlying
        /// port failed. For example, the parameters passed from this System.IO.Ports.SerialPort
        /// object were invalid.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The data bits value is less than 5 or more than 8.
        /// </exception>
        public int DataBits
        {
            get => _dataBits;
            set
            {
                if ((value < 5) || (value > 8))
                {
                    throw new ArgumentOutOfRangeException();
                }

                _dataBits = value;
                NativeConfig();
            }
        }

        /// <summary>
        /// Gets or sets the parity-checking protocol.
        /// </summary>
        /// <exception cref="IOException">The port is in an invalid state. -or- An attempt to set the state of the underlying
        /// port failed. For example, the parameters passed from this System.IO.Ports.SerialPort
        /// object were invalid.</exception>
        public Parity Parity
        {
            get => _parity;
            set
            {
                _parity = value;
                NativeConfig();
            }
        }

        /// <summary>
        /// Gets or sets the Serial Mode
        /// </summary>
        /// <remarks>This is a .NET nanoFrmaework property only</remarks>
        public SerialMode Mode
        {
            get => _mode;
            set
            {
                _mode = value;

                // need to reconfigure device
                NativeConfig();
            }
        }

        /// <summary>
        /// Gets or sets the standard number of stopbits per byte.
        /// </summary>
        /// <exception cref="IOException">The port is in an invalid state. -or- An attempt to set the state of the underlying
        /// port failed. For example, the parameters passed from this System.IO.Ports.SerialPort
        /// object were invalid.</exception>
        public StopBits StopBits
        {
            get => _stopBits;
            set
            {
                _stopBits = value;
                NativeConfig();
            }
        }

        /// <summary>
        /// Gets or sets the port for communications, including but not limited to all available
        /// COM ports.
        /// </summary>
        /// <exception cref="ArgumentException">The System.IO.Ports.SerialPort.PortName property was set to a value with a length
        /// of zero. -or- The System.IO.Ports.SerialPort.PortName property was set to a value
        /// that starts with "\\". -or- The port name was not valid.</exception>
        /// <exception cref="ArgumentNullException">The System.IO.Ports.SerialPort.PortName property was set to null.</exception>
        /// <exception cref="InvalidOperationException">The specified port is open.</exception>
        public string PortName
        {
            get
            {
                if (!_disposed)
                {
                    return _deviceId;
                }

                throw new ObjectDisposedException();
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentException();
                }

                if (_deviceId != value)
                {
                    // First dispose the old one
                    if (_hasBeenOpened)
                    {
                        NativeDispose();
                    }

                    _deviceId = value;
                    _portIndex = (value[3] - 48);
                    NativeInit();
                    _hasBeenOpened = true;
                    _disposed = false;
                }
            }
        }

        /// <summary>
        /// Sets a character to watch for in the incoming data stream.
        /// </summary>
        /// <remarks>
        /// This property is specific to nanoFramework. There is no equivalent in the SerialPort API.
        /// When calling any of the Read function with a buffer, no matter if the requested quantity of bytes hasn't been read, only the specific amount of data will be returned up to the character.
        /// Also if this character is received in the incoming data stream, an event is fired with it's <see cref="SerialData"/> parameter set to <see cref="SerialData.WatchChar"/>.
        /// </remarks>
        public char WatchChar
        {
            set
            {
                _watchChar = value;
                NativeSetWatchChar();
            }
        }

        /// <summary>
        /// Gets or sets the byte encoding for pre- and post-transmission conversion of text.
        /// </summary>
        /// <exception cref="ArgumentNullException">The System.IO.Ports.SerialPort.Encoding property was set to null.</exception>
        /// <exception cref="ArgumentException">
        /// The System.IO.Ports.SerialPort.Encoding property was set to an encoding that
        /// is not System.Text.ASCIIEncoding, System.Text.UTF8Encoding, System.Text.UTF32Encoding,
        /// System.Text.UnicodeEncoding, one of the Windows single byte encodings, or one
        /// of the Windows double byte encodings.
        /// </exception>
        public Encoding Encoding { get; set; } = Encoding.UTF8;

        /// <summary>
        /// Gets a value indicating the open or closed status of the System.IO.Ports.SerialPort
        /// object.
        /// </summary>
        /// <exception cref="ArgumentNullException">The System.IO.Ports.SerialPort.IsOpen value passed is null.</exception>
        /// <exception cref="ArgumentException">The System.IO.Ports.SerialPort.IsOpen value passed is an empty string ("").</exception>

        public bool IsOpen { get => _opened; }

        /// <summary>
        /// Gets or sets the value used to interpret the end of a call to the System.IO.Ports.SerialPort.ReadLine
        /// and System.IO.Ports.SerialPort.WriteLine(System.String) methods.
        /// </summary>
        /// <exception cref="ArgumentException">The property value is empty or the property value is null.</exception>
        public string NewLine
        {
            get => _newLine;
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentException();
                }

                _newLine = value;
            }
        }

        /// <summary>
        /// Gets or sets the number of milliseconds before a time-out occurs when a read
        /// operation does not finish.
        /// </summary>
        /// <exception cref="IOException">The port is in an invalid state. -or- An attempt to set the state of the underlying
        /// port failed. For example, the parameters passed from this System.IO.Ports.SerialPort
        /// object were invalid.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The read time-out value is less than zero and not equal to System.IO.Ports.SerialPort.InfiniteTimeout.</exception>
        public int ReadTimeout
        {
            get => _readTimeout;
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException();
                }

                _readTimeout = value;
            }
        }

        /// <summary>
        /// Gets or sets the number of milliseconds before a time-out occurs when a write
        /// operation does not finish.
        /// </summary>
        /// <exception cref="IOException">The port is in an invalid state. -or- An attempt to set the state of the underlying
        /// port failed. For example, the parameters passed from this System.IO.Ports.SerialPort
        /// object were invalid.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The System.IO.Ports.SerialPort.WriteTimeout value is less than zero and not equal
        /// to System.IO.Ports.SerialPort.InfiniteTimeout.</exception>
        public int WriteTimeout
        {
            get => _writeTimeout;
            set
            {
                if ((value < 0) && (value != InfiniteTimeout))
                {
                    throw new ArgumentOutOfRangeException();
                }

                _writeTimeout = value;
            }
        }

        /// <summary>
        /// Gets or sets the number of bytes in the internal input buffer before a System.IO.Ports.SerialPort.DataReceived
        /// event occurs.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">The System.IO.Ports.SerialPort.ReceivedBytesThreshold value is less than or equal
        /// to zero.</exception>
        public int ReceivedBytesThreshold
        {
            get => _receivedBytesThreshold;
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException();
                }

                _receivedBytesThreshold = value;
            }
        }

        /// <summary>
        /// Gets the underlying System.IO.Stream object for a System.IO.Ports.SerialPort
        /// object.
        /// </summary>
        /// <exception cref="InvalidOperationException">The stream is closed. This can occur because the System.IO.Ports.SerialPort.Open
        /// method has not been called or the System.IO.Ports.SerialPort.Close method has
        /// been called.</exception>
        public Stream BaseStream { get => _stream; }

        /// <summary>
        /// Gets the number of bytes of data in the receive buffer.
        /// </summary>
        /// <returns>The number of bytes of data in the receive buffer.</returns>
        /// <exception cref="InvalidOperationException">The port is not open.</exception>
        public int BytesToRead
        {
            [MethodImpl(MethodImplOptions.InternalCall)]
            get;
        }

        #endregion

        /// <summary>
        /// Indicates that data has been received through a port represented by the System.IO.Ports.SerialPort
        /// object.
        /// </summary>
        public event SerialDataReceivedEventHandler DataReceived
        {
            add
            {
                lock (_syncLock)
                {
                    if (_disposed)
                    {
                        throw new ObjectDisposedException();
                    }

                    SerialDataReceivedEventHandler callbacksOld = _callbacksDataReceivedEvent;
                    SerialDataReceivedEventHandler callbacksNew = (SerialDataReceivedEventHandler)Delegate.Combine(callbacksOld, value);

                    try
                    {
                        _callbacksDataReceivedEvent = callbacksNew;
                    }
                    catch
                    {
                        _callbacksDataReceivedEvent = callbacksOld;

                        throw;
                    }
                }
            }

            remove
            {
                lock (_syncLock)
                {
                    if (_disposed)
                    {
                        throw new ObjectDisposedException();
                    }

                    SerialDataReceivedEventHandler callbacksOld = _callbacksDataReceivedEvent;
                    SerialDataReceivedEventHandler callbacksNew = (SerialDataReceivedEventHandler)Delegate.Remove(callbacksOld, value);

                    try
                    {
                        _callbacksDataReceivedEvent = callbacksNew;
                    }
                    catch
                    {
                        _callbacksDataReceivedEvent = callbacksOld;

                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// Handles internal events and re-dispatches them to the publicly subscribed delegates.
        /// </summary>
        /// <param name="eventType">The <see cref="SerialData"/> event type.</param>

        internal void OnSerialDataReceivedInternal(SerialData eventType)
        {
            SerialDataReceivedEventHandler callbacks = null;

            lock (_syncLock)
            {
                if (!_disposed)
                {
                    callbacks = _callbacksDataReceivedEvent;
                }
            }

            callbacks?.Invoke(this, new SerialDataReceivedEventArgs(eventType));
        }

        /// <summary>
        /// Reads a number of bytes from the System.IO.Ports.SerialPort input buffer and
        /// writes those bytes into a byte array at the specified offset.
        /// </summary>
        /// <param name="buffer">The byte array to write the input to.</param>
        /// <param name="offset">The offset in buffer at which to write the bytes.</param>
        /// <param name="count">The maximum number of bytes to read. Fewer bytes are read if count is greater
        /// than the number of bytes in the input buffer.</param>
        /// <returns>The number of bytes read.</returns>
        /// <exception cref="ArgumentNullException">The buffer passed is null.</exception>
        /// <exception cref="InvalidOperationException">The specified port is not open.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The offset or count parameters are outside a valid region of the buffer being
        /// passed. Either offset or count is less than zero.</exception>
        /// <exception cref="ArgumentException">offset plus count is greater than the length of the buffer.</exception>
        /// <exception cref="System.TimeoutException">No bytes were available to read.</exception>
        public int Read(byte[] buffer, int offset, int count)
        {
            if (!_opened)
            {
                throw new InvalidOperationException();
            }

            if (buffer == null)
            {
                throw new ArgumentNullException();
            }

            if ((offset > buffer.Length) || (count > buffer.Length))
            {
                throw new InvalidOperationException();
            }

            if (offset + count > buffer.Length)
            {
                throw new ArgumentException();
            }

            return (int)NativeRead(buffer, offset, count);
        }

        /// <summary>
        /// Synchronously reads one byte from the System.IO.Ports.SerialPort input buffer.
        /// </summary>
        /// <returns>The byte, cast to an System.Int32, or -1 if the end of the stream has been read.</returns>
        /// <exception cref="InvalidOperationException">The specified port is not open.</exception>
        /// <exception cref="System.ServiceProcess.TimeoutException">The operation did not complete before the time-out period ended. -or- No byte
        /// was read.</exception>
        public int ReadByte()
        {
            if (!_opened)
            {
                throw new InvalidOperationException();
            }

            byte[] toRead = new byte[1];
            NativeRead(toRead, 0, 1);
            return toRead[0];
        }

        /// <summary>
        /// Reads all immediately available bytes, based on the encoding, in both the stream
        /// and the input buffer of the System.IO.Ports.SerialPort object.
        /// </summary>
        /// <returns> The contents of the stream and the input buffer of the System.IO.Ports.SerialPort
        /// object.</returns>
        /// <exception cref="InvalidOperationException">The specified port is not open.</exception>
        public string ReadExisting()
        {
            if (!_opened)
            {
                throw new InvalidOperationException();
            }

            if (BytesToRead == 0)
            {
                return string.Empty;
            }

            byte[] toRead = new byte[BytesToRead];
            NativeRead(toRead, 0, toRead.Length);
            // An exception is thrown if timeout, so we are sure to read only 1 byte properly
            return Encoding.GetString(toRead, 0, toRead.Length);
        }

        /// <summary>
        /// Reads up to the System.IO.Ports.SerialPort.NewLine value in the input buffer.
        /// </summary>
        /// <returns>The contents of the input buffer up to the first occurrence of a System.IO.Ports.SerialPort.NewLine
        /// value.</returns>
        /// <exception cref="InvalidOperationException">The specified port is not open.</exception>
        /// <exception cref="System.TimeoutException">The operation did not complete before the time-out period ended. -or- No bytes
        /// were read.</exception>
        public string ReadLine()
        {
            if (!_opened)
            {
                throw new InvalidOperationException();
            }

            string retReadLine = string.Empty;
            byte[] singleByte = new byte[1];
            bool isNewLine = false;
            ArrayList lineText = new ArrayList();
            byte[] newLineByteArray = Encoding.GetBytes(_newLine);

            DateTime dtTimeout = DateTime.UtcNow.AddMilliseconds(_readTimeout);
            do
            {
                if (BytesToRead > 0)
                {
                    // Read byte by byte
                    NativeRead(singleByte, 0, 1);
                    lineText.Add(singleByte[0]);

                    if (lineText.Count >= newLineByteArray.Length)
                    {
                        isNewLine = true;
                        for (int i = 0; i < newLineByteArray.Length; i++)
                        {
                            if((byte)lineText[lineText.Count - newLineByteArray.Length + i] != newLineByteArray[i])
                            {
                                isNewLine = false;
                                break;
                            }
                        }
                    }
                }

                if ((DateTime.UtcNow >= dtTimeout) && (_readTimeout != InfiniteTimeout))
                {
                    throw new TimeoutException();
                }
            }
            while (!isNewLine);

            byte[] lineTextBytes = new byte[lineText.Count];
            for(int i =0; i<lineText.Count; i++)
            {
                lineTextBytes[i] = (byte)lineText[i];
            }

            retReadLine = Encoding.GetString(lineTextBytes, 0, lineTextBytes.Length);
            return retReadLine;
        }

        /// <summary>
        /// Writes a specified number of bytes to the serial port using data from a buffer.
        /// </summary>
        /// <param name="buffer">The byte array that contains the data to write to the port.</param>
        /// <param name="offset">The zero-based byte offset in the buffer parameter at which to begin copying
        /// bytes to the port.</param>
        /// <param name="count">The number of characters to write.</param>
        /// <exception cref="ArgumentNullException">The buffer passed is null.</exception>
        /// <exception cref="InvalidOperationException">The specified port is not open.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The offset or count parameters are outside a valid region of the buffer being
        /// passed. Either offset or count is less than zero.</exception>
        /// <exception cref="ArgumentException">offset plus count is greater than the length of the buffer.</exception>
        /// <exception cref="TimeoutException">The operation did not complete before the time-out period ended.</exception>
        public void Write(byte[] buffer, int offset, int count)
        {
            if (!_opened)
            {
                throw new InvalidOperationException();
            }

            if (buffer == null)
            {
                throw new ArgumentNullException();
            }

            if ((offset > buffer.Length) || (count > buffer.Length))
            {
                throw new InvalidOperationException();
            }

            if (offset + count > buffer.Length)
            {
                throw new ArgumentException();
            }

            NativeWrite(buffer, offset, count);
            NativeStore();
        }

        /// <summary>
        /// Writes the specified string to the serial port.
        /// </summary>
        /// <param name="text">The string for output.</param>
        /// <exception cref="InvalidOperationException">The specified port is not open.</exception>
        /// <exception cref="ArgumentNullException">text is null.</exception>
        /// <exception cref="TimeoutException">The operation did not complete before the time-out period ended.</exception>
        public void Write(string text)
        {
            if (!_opened)
            {
                throw new InvalidOperationException();
            }

            if (text == null)
            {
                throw new ArgumentException();
            }

            if (text.Length == 0)
            {
                return;
            }

            byte[] toSend = Encoding.GetBytes(text);
            NativeWrite(toSend, 0, toSend.Length);
            NativeStore();
        }

        /// <summary>
        /// Writes a specified number of characters to the serial port using data from a
        /// buffer.
        /// </summary>
        /// <param name="buffer">The character array that contains the data to write to the port.</param>
        /// <param name="offset">The zero-based byte offset in the buffer parameter at which to begin copying
        /// bytes to the port.</param>
        /// <param name="count">The number of characters to write.</param>
        /// <exception cref="ArgumentNullException">The buffer passed is null.</exception>
        /// <exception cref="InvalidOperationException">The specified port is not open.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The offset or count parameters are outside a valid region of the buffer being
        /// passed. Either offset or count is less than zero.</exception>
        /// <exception cref="ArgumentException">offset plus count is greater than the length of the buffer.</exception>
        /// <exception cref="TimeoutException">The operation did not complete before the time-out period ended.</exception>
        public void Write(char[] buffer, int offset, int count)
        {
            if (!_opened)
            {
                throw new InvalidOperationException();
            }

            if (buffer == null)
            {
                throw new ArgumentNullException();
            }

            if ((offset > buffer.Length) || (count > buffer.Length))
            {
                throw new InvalidOperationException();
            }

            if (offset + count > buffer.Length)
            {
                throw new ArgumentException();
            }

            byte[] toSend = Encoding.GetBytes(new string(buffer, offset, count));
            NativeWrite(toSend, 0, toSend.Length);
            NativeStore();
        }

        /// <summary>
        /// Writes the specified string and the System.IO.Ports.SerialPort.NewLine value
        /// to the output buffer.
        /// </summary>
        /// <param name="text">The string to write to the output buffer.</param>
        /// <exception cref="ArgumentNullException">The text parameter is null.</exception>
        /// <exception cref="InvalidOperationException">The specified port is not open.</exception>
        /// <exception cref="TimeoutException">The System.IO.Ports.SerialPort.WriteLine(System.String) method could not write
        /// to the stream.</exception>
        public void WriteLine(string text) => Write(text + NewLine);

        internal static SerialPort FindDevice(int index)
        {
            for (int i = 0; i < SerialDeviceController.DeviceCollection.Count; i++)
            {
                if (((SerialPort)SerialDeviceController.DeviceCollection[i])._portIndex == index)
                {
                    return (SerialPort)SerialDeviceController.DeviceCollection[i];
                }
            }

            return null;
        }

        /// <summary>
        /// Releases the unmanaged resources used by the System.IO.Ports.SerialPort and optionally
        /// releases the managed resources.
        /// </summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged
        /// resources.</param>
        /// <exception cref="IOException">The port is in an invalid state. -or- An attempt to set the state of the underlying
        /// port failed. For example, the parameters passed from this System.IO.Ports.SerialPort
        /// object were invalid.</exception>
        protected void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    if (_opened)
                    {
                        Close();
                    }

                    NativeDispose();
                }

                _disposed = true;
            }
        }

        /// <summary>
        /// Dispose the Serial Port
        /// </summary>
        public void Dispose()
        {
            lock (_syncLock)
            {
                if (!_disposed)
                {
                    Dispose(true);

                    GC.SuppressFinalize(this);
                }
            }
        }

        #region Native methods

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern void NativeDispose();

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern void NativeInit();

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern void NativeConfig();

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal extern void NativeWrite(byte[] buffer, int offset, int count);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal extern uint NativeStore();

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal extern uint NativeRead(byte[] buffer, int offset, int count);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal extern void NativeSetWatchChar();

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern string GetDeviceSelector();
        #endregion
    }
}