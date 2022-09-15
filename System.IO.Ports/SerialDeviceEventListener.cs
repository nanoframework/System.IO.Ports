// Copyright (c) .NET Foundation and Contributors
// See LICENSE file in the project root for full license information.

using System.Collections;
using nanoFramework.Runtime.Events;

namespace System.IO.Ports
{
    internal class SerialDeviceEventListener : IEventProcessor, IEventListener
    {
        // Map of serial device numbers to SerialDevice objects.
        private readonly ArrayList _serialDevicesMap = new();

        public SerialDeviceEventListener()
        {
            EventSink.AddEventProcessor(EventCategory.SerialDevice, this);
            EventSink.AddEventListener(EventCategory.SerialDevice, this);
        }

        public BaseEvent ProcessEvent(uint data1, uint data2, DateTime time)
        {
            return new SerialDataEvent
            {
                // Data1 is packed by PostManagedEvent, so we need to unpack the high word.
                SerialDeviceIndex = (int)(data1 >> 16),
                Event = (SerialData)data2
            };
        }

        public void InitializeForEventSource()
        {
            // This method has to exist
        }

        public bool OnEvent(BaseEvent ev)
        {
            var serialDataEvent = (SerialDataEvent)ev;
            SerialPort device = null;

            lock (_serialDevicesMap.SyncRoot)
            {
                device = FindSerialDevice(serialDataEvent.SerialDeviceIndex);
            }

            // Avoid calling this under a lock to prevent a potential lock inversion.
            if (device != null)
            {
                device.OnSerialDataReceivedInternal(serialDataEvent.Event);
            }

            return true;
        }

        public void AddSerialDevice(SerialPort device)
        {
            lock (_serialDevicesMap.SyncRoot)
            {
                _serialDevicesMap.Add(device);
            }
        }

        public void RemoveSerialDevice(int index)
        {
            lock (_serialDevicesMap.SyncRoot)
            {
                var device = FindSerialDevice(index);

                if (device != null)
                {
                    _serialDevicesMap.Remove(device);
                }
            }
        }

        private SerialPort FindSerialDevice(int number)
        {
            for (int i = 0; i < _serialDevicesMap.Count; i++)
            {
                if (((SerialPort)_serialDevicesMap[i])._portIndex == number)
                {
                    return (SerialPort)_serialDevicesMap[i];
                }
            }

            return null;
        }
    }
}
