// Copyright (c) .NET Foundation and Contributors
// See LICENSE file in the project root for full license information.

using System.Collections;

namespace System.IO.Ports
{
    /// <summary>
    /// This class is used to keep tabs on what serial ports are open.
    /// </summary>
    internal static class SerialDeviceController
    {
        // this is used as the lock object 
        // a lock is required because multiple threads can access the SerialDevice controller
        [System.Diagnostics.DebuggerBrowsable(Diagnostics.DebuggerBrowsableState.Never)]
        private static object _syncLock;

        // backing field for DeviceCollection
        private static ArrayList _deviceCollection;

        /// <summary>
        /// Gets or sets the device collection associated with this <see cref="SerialDeviceController"/>.
        /// </summary>
        /// <remarks>
        /// This collection is for internal use only.
        /// </remarks>
        internal static ArrayList DeviceCollection
        {
            get
            {
                if (_deviceCollection == null)
                {
                    if (_syncLock == null)
                    {
                        _syncLock = new object();
                    }

                    lock (_syncLock)
                    {
                        if (_deviceCollection == null)
                        {
                            _deviceCollection = new ArrayList();
                        }
                    }
                }

                return _deviceCollection;
            }

            set
            {
                _deviceCollection = value;
            }
        }
    }
}
