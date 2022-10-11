// Copyright (c) .NET Foundation and Contributors
// See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using System.Threading;

namespace System.IO.Ports
{
    /// <summary>
    /// Represents a serial port resource.
    /// </summary>
    public sealed class SerialPort : IDisposable
    {
        // default new line
        [System.Diagnostics.DebuggerBrowsable(Diagnostics.DebuggerBrowsableState.Never)]
        private const string _defaultNewLine = "\n";

        [System.Diagnostics.DebuggerBrowsable(Diagnostics.DebuggerBrowsableState.Never)]
        private static readonly SerialDeviceEventListener s_eventListener = new();

        private bool _disposed;

        // this is used as the lock object 
        // a lock is required because multiple threads can access the SerialPort
        [System.Diagnostics.DebuggerBrowsable(Diagnostics.DebuggerBrowsableState.Never)]
        private readonly object _syncLock = new();

        // flag to signal an open serial port
        private bool _opened;

        private int _writeTimeout = Timeout.Infinite;
        private int _readTimeout = Timeout.Infinite;

        // default threshold is 1
        private int _receivedBytesThreshold = 1;
        private int _baudRate;
        private Handshake _handshake = Handshake.None;
        private StopBits _stopBits;
        private int _dataBits;
        private Parity _parity;
        private SerialMode _mode = SerialMode.Normal;
        private readonly string _deviceId;
        internal int _portIndex;

#pragma warning disable S4487 // need this to be used in native code
        private char _watchChar;
#pragma warning restore S4487 // Unread "private" fields should be removed

        private SerialDataReceivedEventHandler _callbacksDataReceivedEvent = null;
        private SerialStream _stream;
        private string _newLine;
        private int _bufferSize = 256;

        /// <summary>
        /// Initializes a new instance of the <see cref="SerialPort"/> class using the
        /// specified port name, baud rate, parity bit, data bits, and stop bit.
        /// </summary>
        /// <param name="portName">The port to use (for example, COM1).</param>
        /// <param name="baudRate">The baud rate.</param>
        /// <param name="parity">One of the <see cref="Parity"/> values.</param>
        /// <param name="dataBits">The data bits value.</param>
        /// <param name="stopBits">One of the <see cref="StopBits"/> values.</param>
        /// <exception cref="IOException">The specified port could not be found or opened.</exception>
        /// <exception cref="ArgumentException">The specified port is already opened.</exception>
        public SerialPort(
            string portName,
            int baudRate = 9600,
            Parity parity = Parity.None,
            int dataBits = 8,
            StopBits stopBits = StopBits.One)
        {
            // the UART name is an ASCII string with the COM port name in format 'COMn'
            // need to grab 'n' from the string and convert that to the integer value from the ASCII code (do this by subtracting 48 from the char value)
            _portIndex = portName[3] - 48;

            var device = FindDevice(_portIndex);

            if (device == null)
            {
                _deviceId = portName;
                _baudRate = baudRate;
                _parity = parity;
                _dataBits = dataBits;
                _stopBits = stopBits;
                _newLine = _defaultNewLine;

                // add serial device to collection
                SerialDeviceController.DeviceCollection.Add(this);
            }
            else
            {
                // this device already exists throw an exception
#pragma warning disable S3928 // OK to throw this exception without details on the argument.
                throw new ArgumentException();
#pragma warning restore S3928 // Parameter names used into ArgumentException constructors should match an existing one 
            }
        }

        /// <summary>
        /// Opens a new serial port connection.
        /// </summary>
        /// <exception cref="InvalidOperationException">The specified port on the current instance of the <see cref="SerialPort"/>.
        /// is already open.</exception>
        /// <exception cref="ArgumentException">One (or more) of the properties set to configure this <see cref="SerialPort"/> are invalid.</exception>
        /// <exception cref="OutOfMemoryException">Failed to allocate the request amount of memory for the work buffer.</exception>
        public void Open()
        {
            if (!_opened)
            {
                _disposed = false;

                NativeInit();

                // add the serial device to the event listener in order to receive the callbacks from the native interrupts
                s_eventListener.AddSerialDevice(this);

                _stream = new SerialStream(this);

                _opened = true;
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        /// <summary>
        /// Closes the port connection, sets the <see cref="IsOpen"/> property
        /// to false, and disposes of the internal <see cref="Stream"/> object.
        /// </summary>
        /// <exception cref="IOException">The port is in an invalid state. -or- An attempt to set the state of the underlying
        /// port failed. For example, the parameters passed from this <see cref="SerialPort"/>
        /// object were invalid.</exception>
        public void Close()
        {
            if (_opened)
            {
                _stream.Flush();

                // remove the pin from the event listener
                s_eventListener.RemoveSerialDevice(_portIndex);

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
        /// port failed. For example, the parameters passed from this <see cref="SerialPort"/>
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

                // reconfig needed, if opened
                if (_opened)
                {
                    NativeConfig();
                }
            }
        }

        /// <summary>
        /// Gets or sets the handshaking protocol for serial port transmission of data using
        /// a value from <see cref="Ports.Handshake"/>.
        /// </summary>
        /// <exception cref="IOException">
        /// The port is in an invalid state. -or- An attempt to set the state of the underlying
        /// port failed. For example, the parameters passed from this <see cref="SerialPort"/>
        /// object were invalid.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">The value passed is not a valid value in the <see cref="Ports.Handshake"/> enumeration.</exception>
        public Handshake Handshake
        {
            get => _handshake;

            set
            {
                _handshake = value;

                // reconfig needed, if opened
                if (_opened)
                {
                    NativeConfig();
                }
            }
        }

        /// <summary>
        /// Gets or sets the standard length of data bits per byte.
        /// </summary>
        /// <exception cref="IOException">
        /// The port is in an invalid state. -or- An attempt to set the state of the underlying
        /// port failed. For example, the parameters passed from this <see cref="SerialPort"/>
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

                // reconfig needed, if opened
                if (_opened)
                {
                    NativeConfig();
                }
            }
        }

        /// <summary>
        /// Gets or sets the parity-checking protocol.
        /// </summary>
        /// <exception cref="IOException">The port is in an invalid state. -or- An attempt to set the state of the underlying
        /// port failed. For example, the parameters passed from this <see cref="SerialPort"/>
        /// object were invalid.</exception>
        public Parity Parity
        {
            get => _parity;

            set
            {
                _parity = value;

                // reconfig needed, if opened
                if (_opened)
                {
                    NativeConfig();
                }
            }
        }

        /// <summary>
        /// Gets or sets the Serial Mode.
        /// </summary>
        /// <remarks>This is a .NET nanoFramework property only.</remarks>
        public SerialMode Mode
        {
            get => _mode;

            set
            {
                _mode = value;

                // reconfig needed, if opened
                if (_opened)
                {
                    NativeConfig();
                }
            }
        }

        /// <summary>
        /// Gets or sets the standard number of stopbits per byte.
        /// </summary>
        /// <exception cref="IOException">The port is in an invalid state. -or- An attempt to set the state of the underlying
        /// port failed. For example, the parameters passed from this <see cref="SerialPort"/>
        /// object were invalid.</exception>
        public StopBits StopBits
        {
            get => _stopBits;

            set
            {
                _stopBits = value;

                // reconfig needed, if opened
                if (_opened)
                {
                    NativeConfig();
                }
            }
        }

        /// <summary>
        /// Gets the port for communications.
        /// </summary>
        /// <remarks>
        /// .NET nanoFramework doesn't support changing the port.
        /// </remarks>
        public string PortName
        {
            get
            {
                return _deviceId;
            }
        }

        /// <summary>
        /// Sets a character to watch for in the incoming data stream.
        /// </summary>
        /// <remarks>
        /// This property is specific to .NET nanoFramework. There is no equivalent in the System.IO.Ports API.
        /// When calling any of the Read function with a buffer, no matter if the requested quantity of bytes hasn't been read, only the specific amount of data will be returned up to the character.
        /// Also if this character is received in the incoming data stream, an event is fired with it's <see cref="SerialData"/> parameter set to <see cref="SerialData.WatchChar"/>.
        /// </remarks>
        public char WatchChar
        {
            set
            {
                _watchChar = value;

                // update native, if opened
                if (_opened)
                {
                    NativeSetWatchChar();
                }
            }

            get
            {
                return _watchChar;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the <see cref="SerialPort"/> is open or closed.
        /// </summary>
        /// <exception cref="ArgumentNullException">The <see cref="IsOpen"/> value passed is null.</exception>
        /// <exception cref="ArgumentException">The <see cref="IsOpen"/> value passed is an empty string ("").</exception>
        public bool IsOpen { get => _opened; }

        /// <summary>
        /// Gets or sets the value used to interpret the end of a call to the <see cref="ReadLine"/>
        /// and <see cref="WriteLine"/>(System.String) methods.
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
        /// port failed. For example, the parameters passed from this <see cref="SerialPort"/>
        /// object were invalid.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The read time-out value is less than zero and not equal to <see cref="Timeout.Infinite"/>.</exception>
        public int ReadTimeout
        {
            get => _readTimeout;

            set
            {
                if (value < 0 && value != Timeout.Infinite)
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
        /// port failed. For example, the parameters passed from this <see cref="SerialPort"/>
        /// object were invalid.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The read time-out value is less than zero and not equal to <see cref="Timeout.Infinite"/>.</exception>
        public int WriteTimeout
        {
            get => _writeTimeout;

            set
            {
                if (value < 0 && value != Timeout.Infinite)
                {
                    throw new ArgumentOutOfRangeException();
                }

                _writeTimeout = value;
            }
        }

        /// <summary>
        /// Gets or sets the number of bytes in the internal input buffer before a <see cref="DataReceived"/>
        /// event occurs.
        /// </summary>
        /// <value>The number of bytes in the internal input buffer before a <see cref="DataReceived"/> event is fired. The default is 1.</value>
        /// <exception cref="ArgumentOutOfRangeException">The <see cref="ReceivedBytesThreshold"/> value is less than or equal
        /// to zero.</exception>
        public int ReceivedBytesThreshold
        {
            get => _receivedBytesThreshold;

            set
            {
                NativeReceivedBytesThreshold(value);
            }
        }

        /// <summary>
        /// Gets the underlying <see cref="Stream"/> object for a <see cref="SerialPort"/>
        /// object.
        /// </summary>
        /// <exception cref="InvalidOperationException">The stream is closed. This can occur because the <see cref="Open"/>
        /// method has not been called or the <see cref="Close"/> method has
        /// been called.</exception>
        public Stream BaseStream { get => _stream; }

        /// <summary>
        /// Gets the number of bytes of data in the receive buffer.
        /// </summary>
        /// <returns>The number of bytes of data in the receive buffer.</returns>
        /// <exception cref="InvalidOperationException">The port is not open.</exception>
        public extern int BytesToRead
        {
            [MethodImpl(MethodImplOptions.InternalCall)]
            get;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the logic level of the RX and TX signals are inverted. 
        /// </summary>
        /// <exception cref="InvalidOperationException">Trying to set this property when the <see cref="SerialPort"/> is already opened and the driver doesn't support it.</exception>
        /// <exception cref="NotSupportedException">Trying to set this property on a target that does not support signal inversion.</exception>
        /// <remarks>
        /// When the signal levels are not inverted (reads <see langword="false"/>) the RX, TX pins use the standard logic levels (VDD = 1/idle, GND = 0/mark).
        /// Setting this property to <see langword="true"/>, will invert those signal levels, which will become inverted (VDD = 0/mark, GND= 1/idle).
        /// Some targets may not support this setting and accessing it will throw a <see cref="NotSupportedException"/> exception.
        /// This is a .NET nanoFramework property only. Doesn't exist on other .NET platforms.
        /// </remarks>
        public extern bool InvertSignalLevels
        {
            [MethodImpl(MethodImplOptions.InternalCall)]
            get;

            [MethodImpl(MethodImplOptions.InternalCall)]
            set;
        }

        /// <summary>
        /// Gets or sets the size of the SerialPort input buffer.
        /// </summary>
        /// <value>The size of the input buffer. The default is 256.</value>
        /// <exception cref="ArgumentOutOfRangeException">The <see cref="ReadBufferSize"/> value is less than or equal to zero.</exception>
        /// <exception cref="InvalidOperationException">The <see cref="ReadBufferSize"/> property was set while the stream was open.</exception>
        /// <remarks>
        /// <para>
        /// Implementation of this property for .NET nanoFramework it's slightly different from .NET.
        /// </para>
        /// <para>
        /// - There is only one work buffer which is used for transmission and reception.
        /// </para>
        /// <para>
        /// - When the <see cref="SerialPort"/> is <see cref="Open"/> the driver will try to allocate the requested memory for the buffer. On failure to do so, an <see cref="OutOfMemoryException"/> exception will be throw and the <see cref="Open"/> operation will fail.
        /// </para>
        /// </remarks>
        public int ReadBufferSize
        {
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException();
                }

                if (_opened)
                {
                    throw new InvalidOperationException();
                }

                _bufferSize = value;
            }

            get
            {
                return _bufferSize;
            }
        }

        /// <summary>
        /// Gets or sets the size of the serial port output buffer.
        /// </summary>
        /// <value>The size of the output buffer. The default is 256.</value>
        /// <exception cref="ArgumentOutOfRangeException">The <see cref="WriteBufferSize"/> value is less than or equal to zero.</exception>
        /// <exception cref="InvalidOperationException">The <see cref="WriteBufferSize"/> property was set while the stream was open.</exception>
        /// <remarks>
        /// <para>
        /// Implementation of this property for .NET nanoFramework it's slightly different from .NET.
        /// </para>
        /// <para>
        /// - There is only one work buffer which is used for transmission and reception.
        /// </para>
        /// <para>
        /// - When the <see cref="SerialPort"/> is <see cref="Open"/> the driver will try to allocate the requested memory for the buffer. On failure to do so, an <see cref="OutOfMemoryException"/> exception will be throw and the <see cref="Open"/> operation will fail.
        /// </para>
        /// </remarks>
        public int WriteBufferSize
        {
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException();
                }

                if (_opened)
                {
                    throw new InvalidOperationException();
                }

                _bufferSize = value;
            }

            get
            {
                return _bufferSize;
            }
        }

        #endregion

        /// <summary>
        /// Indicates that data has been received through a port represented by the <see cref="SerialPort"/>
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
#pragma warning disable S3877 // OK to throw this here
                        throw new ObjectDisposedException();
#pragma warning restore S3877 // Exceptions should not be thrown from unexpected methods
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
#pragma warning disable S3877 // OK to throw this here
                        throw new ObjectDisposedException();
#pragma warning restore S3877 // Exceptions should not be thrown from unexpected methods
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
        /// Reads a number of bytes from the <see cref="SerialPort"/> input buffer and
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
        /// <exception cref="ArgumentException">Offset plus count is greater than the length of the buffer.</exception>
        /// <exception cref="TimeoutException">No bytes were available to read.</exception>
        [MethodImpl(MethodImplOptions.InternalCall)]
#pragma warning disable S4200 // OK to make a direct call
        public extern int Read(byte[] buffer, int offset, int count);
#pragma warning restore S4200 // Native methods should be wrapped

        /// <summary>
        /// Synchronously reads one byte from the <see cref="SerialPort"/> input buffer.
        /// </summary>
        /// <returns>The byte, cast to an <see cref="int"/>, or -1 if the end of the stream has been read.</returns>
        /// <exception cref="InvalidOperationException">The specified port is not open.</exception>
        /// <exception cref="TimeoutException">The operation did not complete before the time-out period ended. -or- No byte
        /// was read.</exception>
        public int ReadByte()
        {
            byte[] buffer = new byte[1];

            Read(buffer, 0, 1);

            return buffer[0];
        }

        /// <summary>
        /// Reads all immediately available bytes, based on the encoding, in both the stream
        /// and the input buffer of the <see cref="SerialPort"/> object.
        /// </summary>
        /// <returns> The contents of the stream and the input buffer of the <see cref="SerialPort"/>
        /// object.</returns>
        /// <exception cref="InvalidOperationException">The specified port is not open.</exception>
        [MethodImpl(MethodImplOptions.InternalCall)]
#pragma warning disable S4200 // OK to make a direct call
        public extern string ReadExisting();
#pragma warning restore S4200 // Native methods should be wrapped

        /// <summary>
        /// Reads up to the <see cref="NewLine"/> value in the input buffer.
        /// </summary>
        /// <returns>The contents of the input buffer up to the first occurrence of a <see cref="NewLine"/> value.</returns>
        /// <exception cref="InvalidOperationException">The specified port is not open.</exception>
        /// <exception cref="TimeoutException">The operation did not complete before the time-out period ended. -or- No bytes
        /// were read.</exception>
        [MethodImpl(MethodImplOptions.InternalCall)]
#pragma warning disable S4200 // OK to make a direct call
        public extern string ReadLine();
#pragma warning restore S4200 // Native methods should be wrapped

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
        /// <exception cref="ArgumentException">Offset plus count is greater than the length of the buffer.</exception>
        /// <exception cref="TimeoutException">The operation did not complete before the time-out period ended.</exception>
        [MethodImpl(MethodImplOptions.InternalCall)]
#pragma warning disable S4200 // OK to make a direct call
        public extern void Write(byte[] buffer, int offset, int count);
#pragma warning restore S4200 // Native methods should be wrapped

        /// <summary>
        /// Writes a byte to the serial port.
        /// </summary>
        /// <param name="value">The byte to write to the port.</param>
        /// <exception cref="InvalidOperationException">The specified port is not open.</exception>
        /// <exception cref="TimeoutException">The operation did not complete before the time-out period ended.</exception>
        public void WriteByte(byte value)
        {
            Write(
                new byte[] { value },
                0,
                1);
        }

        /// <summary>
        /// Writes the specified string to the serial port.
        /// </summary>
        /// <param name="text">The string for output.</param>
        /// <exception cref="InvalidOperationException">The specified port is not open.</exception>
        /// <exception cref="ArgumentNullException">Text is null.</exception>
        /// <exception cref="TimeoutException">The operation did not complete before the time-out period ended.</exception>
#pragma warning disable S4200 // OK to make a direct call
        public void Write(string text) => NativeWriteString(text, false);
#pragma warning restore S4200 // Native methods should be wrapped

        /// <summary>
        /// Writes the specified string and the <see cref="NewLine"/> value
        /// to the output buffer.
        /// </summary>
        /// <param name="text">The string to write to the output buffer.</param>
        /// <exception cref="ArgumentNullException">The text parameter is null.</exception>
        /// <exception cref="InvalidOperationException">The specified port is not open.</exception>
        /// <exception cref="TimeoutException">The <see cref="WriteLine"/>(System.String) method could not write
        /// to the stream.</exception>
#pragma warning disable S4200 // OK to make a direct call
        public void WriteLine(string text) => NativeWriteString(text, true);
#pragma warning restore S4200 // Native methods should be wrapped

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
        /// Destructor.
        /// </summary>
        ~SerialPort()
        {
            Dispose(false);
        }

        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="SerialPort"/> and optionally
        /// releases the managed resources.
        /// </summary>
        /// <param name="disposing">True to release both managed and unmanaged resources; false to release only unmanaged
        /// resources.</param>
        /// <exception cref="IOException">The port is in an invalid state. -or- An attempt to set the state of the underlying
        /// port failed. For example, the parameters passed from this <see cref="SerialPort"/>
        /// object were invalid.</exception>
        internal void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    if (_opened)
                    {
                        Close();
                    }

                    // find device
                    var device = FindDevice(_portIndex);

                    if (device != null)
                    {
                        // remove device from collection
                        SerialDeviceController.DeviceCollection.Remove(device);
                    }

                    NativeDispose();
                }

                _disposed = true;
            }
        }

        /// <summary>
        /// Dispose the Serial Port.
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
        internal extern void NativeSetWatchChar();

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal extern void NativeWriteString(string text, bool addNewLine);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern string GetDeviceSelector();

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal extern void NativeReceivedBytesThreshold(int value);

        #endregion
    }
}
