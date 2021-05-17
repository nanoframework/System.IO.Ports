//
// Copyright (c) .NET Foundation and Contributors
// See LICENSE file in the project root for full license information.
//

using System.Collections;

namespace System.IO.Ports
{
    /// <summary>
    /// This class is used to keep tabs on what serial ports are open.
    /// </summary>
    internal sealed class SerialDeviceController
    {
        // this is used as the lock object 
        // a lock is required because multiple threads can access the SerialDevice controller
        private static object _syncLock;

        // we can have only one instance of the SerialDeviceController
        // need to do a lazy initialization of this field to make sure it exists when called elsewhere.
        private static SerialDeviceController s_instance;

        // backing field for DeviceCollection
        private static ArrayList s_deviceCollection;

        /// <summary>
        /// Device collection associated with this <see cref="SerialDeviceController"/>.
        /// </summary>
        /// <remarks>
        /// This collection is for internal use only.
        /// </remarks>
        internal static ArrayList DeviceCollection
        {
            get
            {
                if (s_deviceCollection == null)
                {
                    if (_syncLock == null)
                    {
                        _syncLock = new object();
                    }

                    lock (_syncLock)
                    {
                        if (s_deviceCollection == null)
                        {
                            s_deviceCollection = new ArrayList();
                        }
                    }
                }

                return s_deviceCollection;
            }

            set
            {
                s_deviceCollection = value;
            }
        }

        /// <summary>
        /// Gets the default serial device controller for the system.
        /// </summary>
        /// <returns>The default GPIO controller for the system, or null if the system has no GPIO controller.</returns>
        internal static SerialDeviceController GetDefault()
        {
            if (s_instance == null)
            {
                if (_syncLock == null)
                {
                    _syncLock = new object();
                }

                lock (_syncLock)
                {
                    if (s_instance == null)
                    {
                        s_instance = new SerialDeviceController();
                    }
                }
            }

            return s_instance;
        }
    }
}
