//
// Copyright (c) .NET Foundation and Contributors
// See LICENSE file in the project root for full license information.
//

using nanoFramework.Hardware.Esp32; // TODO: only include if platform needs it?!
using nanoFramework.TestFramework;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Text;
using System.Threading;

namespace UnitTestsSerialPort
{
    [TestClass]
    public class SerialTests
    {
        static SerialPort _serOne;
        static SerialPort _serTwo;

        // TODO: only include if platform needs it?!
        [Setup]
        public void SetupComPorts_ESP32()
        {
            OutputHelper.WriteLine("Setting up tests for an ESP32...");
            try
            {
                OutputHelper.WriteLine("Please adjust for your own usage. If you need another hardware, please add the proper nuget and adjust as well");
                Configuration.SetPinFunction(32, DeviceFunction.COM2_RX);
                Configuration.SetPinFunction(33, DeviceFunction.COM2_TX);
                Configuration.SetPinFunction(12, DeviceFunction.COM2_RTS);
                Configuration.SetPinFunction(13, DeviceFunction.COM2_CTS);

                Configuration.SetPinFunction(16, DeviceFunction.COM3_RX);
                Configuration.SetPinFunction(17, DeviceFunction.COM3_TX);
                Configuration.SetPinFunction(27, DeviceFunction.COM3_RTS);
                Configuration.SetPinFunction(14, DeviceFunction.COM3_CTS);

                OutputHelper.WriteLine("You will need to connect:");
                OutputHelper.WriteLine("  COM2 RX  <-> COM3 TX");
                OutputHelper.WriteLine("  COM2 TX  <-> COM3 RX");
                OutputHelper.WriteLine("  COM2 RTS <-> COM3 CTS");
                OutputHelper.WriteLine("  COM2 CTS <-> COM3 RTS");
                _serOne = new SerialPort("COM2");
                _serTwo = new SerialPort("COM3");
                OutputHelper.WriteLine("SerialPorts created, trying to open them");
                _serOne.Open();
                OutputHelper.WriteLine("SerialPort One COM2 opened.");
                _serTwo.Open();
                OutputHelper.WriteLine("SerialPort Two COM3 opened.");
                OutputHelper.WriteLine("All SerialPorts opened, will close them");
                // Wait a bit just to make sure and close them all
                Thread.Sleep(100);
                EnsurePortsClosed();
                OutputHelper.WriteLine("SerialPorts Closed.");
            }
            catch
            {
                Assert.SkipTest("Serial Ports not supported in this platform or not properly configured");
            }
        }

        //[Setup]
        //public void SetupComPorts_ChibOs_STM32F769I_Disco()
        //{
        //    OutputHelper.WriteLine("Setting up tests for an STM32F769I...");
        //    try
        //    {
        //        Debug.WriteLine("Please adjust for your own usage. If you need another hardware, please add the proper nuget and adjust as well");

        //        OutputHelper.WriteLine("You will need to connect:");
        //        OutputHelper.WriteLine("  COM5 (PD2) RX  <-> COM6 (PC6) TX");
        //        OutputHelper.WriteLine("  COM5 (PC12) TX  <-> COM6 (PC7) RX");
        //        // OutputHelper.WriteLine("  COM5 RTS <-> COM6 CTS");
        //        // OutputHelper.WriteLine("  COM5 CTS <-> COM6 RTS");
        //        _serOne = new SerialPort("COM5");
        //        _serTwo = new SerialPort("COM6");
        //        OutputHelper.WriteLine("SerialPorts created, trying to open them");
        //        _serOne.Open();
        //        OutputHelper.WriteLine("SerialPort One COM5 opened");
        //        _serTwo.Open();
        //        OutputHelper.WriteLine("SerialPort Two COM6 opened");
        //        OutputHelper.WriteLine("SarialPorts opened, will close them");
        //        // Wait a bit just to make sure and close them all
        //        Thread.Sleep(100);
        //        EnsurePortsClosed();
        //        OutputHelper.WriteLine("SerialPorts Closed.");
        //    }
        //    catch
        //    {
        //        Assert.SkipTest("Serial Ports not supported in this platform or not properly configured");
        //    }
        //}

        [TestMethod]
        public void GetPortNamesTest()
        {
            var ports = SerialPort.GetPortNames();
            OutputHelper.WriteLine("Available SerialPorts:");
            foreach (string port in ports)
            {
                OutputHelper.WriteLine($"  {port}");
            }
        }

        [TestMethod]
        public void BasicReadWriteTests()
        {
            // Arrange
            EnsurePortsOpen();
            _serOne.WriteTimeout = 1000;
            _serOne.ReadTimeout = 1000;
            _serTwo.WriteTimeout = 1000;
            _serTwo.ReadTimeout = 1000;
            byte[] toSend = new byte[] { 0x42, 0xAA, 0x11, 0x00 };
            byte[] toReceive = new byte[toSend.Length];
            // Act
            _serOne.Write(toSend, 0, toSend.Length);
            // Give some time for the first com to send the data
            Thread.Sleep(100);
            _serTwo.Read(toReceive, 0, toReceive.Length);
            // Assert
            for (int i = 0; i < toSend.Length; i++)
            {
                Assert.Equal(toSend[i], toReceive[i]);
            }
        }

        [TestMethod]
        public void VerifyDefaultReadLineCharacter()
        {
            // Arrange
            EnsurePortsOpen();
            _serOne.WriteTimeout = 1000;
            _serOne.ReadTimeout = 1000;
            _serTwo.WriteTimeout = 1000;
            _serTwo.ReadTimeout = 1000;
            string toSend = "This is a simple test for verifying the default readline character \r\n \\r";
            // Act
            _serOne.Write(toSend);
            string toReceive = _serTwo.ReadLine();
            // Assert
            Assert.Equal(toSend, toReceive + "\\r" + _serOne.NewLine);
        }

        [TestMethod]
        public void WriteAndReadStringTests()
        {
            // Arrange
            EnsurePortsOpen();
            _serOne.WriteTimeout = 1000;
            _serOne.ReadTimeout = 1000;
            _serTwo.WriteTimeout = 1000;
            _serTwo.ReadTimeout = 1000;
            string toSend = "Hi, this is a simple test with string";
            // Act
            _serOne.WriteLine(toSend);
            string toReceive = _serTwo.ReadLine();
            // Assert
            Assert.Equal(toSend + _serOne.NewLine, toReceive);
        }

        [TestMethod]
        public void ReadMultipleLinesTests()
        {
            // Arrange
            EnsurePortsOpen();
            _serOne.WriteTimeout = 1000;
            _serOne.ReadTimeout = 1000;
            _serTwo.WriteTimeout = 1000;
            _serTwo.ReadTimeout = 1000;
            string toSend = $"Hi, this is a simple test with string{_serOne.NewLine}And with a second line{_serOne.NewLine}Only line by line should be read{_serOne.NewLine}";
            // Act
            _serOne.WriteLine(toSend);
            string toReceive = _serTwo.ReadLine();
            // Assert
            Assert.Equal($"Hi, this is a simple test with string{_serOne.NewLine}", toReceive);
            toReceive = _serTwo.ReadLine();
            Assert.Equal($"And with a second line{_serOne.NewLine}", toReceive);
            toReceive = _serTwo.ReadLine();
            Assert.Equal($"Only line by line should be read{_serOne.NewLine}", toReceive);
        }

        [TestMethod]
        public void CheckReadByteSize()
        {
            // Arrange
            EnsurePortsOpen();
            EnsurePortEmpty(_serTwo);

            string toSend = $"I ❤ nanoFramework{_serOne.NewLine}";
            int enc = Encoding.UTF8.GetBytes(toSend).Length;
            // Act
            _serOne.Write(toSend);
            // Wait a bit to have data sent
            Thread.Sleep(100);
            // Assert
            Assert.Equal(enc, _serTwo.BytesToRead);
            // Read remaining
            string sent = _serTwo.ReadExisting();
            Assert.Equal(toSend, sent);
        }

        [TestMethod]
        public void CheckReadLineWithoutAnything()
        {
            long dtOrigine = DateTime.UtcNow.Ticks;
            Assert.Throws(typeof(TimeoutException), () =>
            {
                // Arrange
                EnsurePortsOpen();
                _serTwo.ReadTimeout = 2000;
                EnsurePortEmpty(_serTwo);

                // Act            
                var nothingToRead = _serTwo.ReadLine();
                // Assert
            });
            long dtTimeout = DateTime.UtcNow.Ticks;
            Assert.True(dtTimeout >= dtOrigine + _serTwo.ReadTimeout * TimeSpan.TicksPerMillisecond);
        }

        [TestMethod]
        public void CheckReadTimeoutTest()
        {
            Assert.Throws(typeof(TimeoutException), () =>
            {
                // Arrange
                EnsurePortsOpen();
                _serTwo.ReadTimeout = 1000;
                // Ensure nothing to read, if yes, read all
                EnsurePortEmpty(_serTwo);

                byte[] toReadTimeout = new byte[5];
                _serTwo.Read(toReadTimeout, 0, toReadTimeout.Length);
            });
        }

        [TestMethod]
        public void ReadByteWriteByteTests()
        {
            // Arrange
            EnsurePortsOpen();
            _serOne.WriteTimeout = 1000;
            _serOne.ReadTimeout = 1000;
            _serTwo.WriteTimeout = 1000;
            _serTwo.ReadTimeout = 1000;
            EnsurePortEmpty(_serOne);
            EnsurePortEmpty(_serTwo);

            byte[] toWrite = new byte[] { 0, 42, 0 };
            byte toRead;
            // Act
            _serOne.Write(toWrite, 1, 1);
            // Wait to make sure it will be send
            Thread.Sleep(100);
            toRead = (byte)_serTwo.ReadByte();
            // Assert
            Assert.Equal(toWrite[1], toRead);
        }

        [TestMethod]
        public void CheckBytesAvailableTest()
        {
            // Arrange
            EnsurePortsOpen();
            _serOne.WriteTimeout = 1000;
            _serOne.ReadTimeout = 1000;
            _serTwo.WriteTimeout = 1000;
            _serTwo.ReadTimeout = 1000;
            EnsurePortEmpty(_serOne);
            EnsurePortEmpty(_serTwo);
            byte[] lotsOfBytes = new byte[42];
            // Act
            _serOne.Write(lotsOfBytes, 0, lotsOfBytes.Length);
            // Wait to make sure it will be send
            Thread.Sleep(100);
            var numBytes = _serTwo.BytesToRead;
            // Clean
            EnsurePortEmpty(_serTwo);
            // Assert
            Assert.Equal(lotsOfBytes.Length, numBytes);
        }

        [TestMethod]
        public void TryReadWriteWhileClosed()
        {
            EnsurePortsClosed();
            Assert.Throws(typeof(InvalidOperationException), () =>
            {
                _serOne.Write("Something");
            });

            Assert.Throws(typeof(InvalidOperationException), () =>
            {
                byte[] something = new byte[5];
                _serOne.Read(something, 0, something.Length);
            });
        }

        [TestMethod]
        public void AdjustBaudRateTests()
        {
            // Arrange
            _serOne.BaudRate = 115200;
            _serTwo.BaudRate = 115200;
            // Act and Assert
            SendAndReceiveBasic();
        }

        [TestMethod]
        public void AdjustHandshakeTests()
        {
            // Arrange
            _serOne.Handshake = Handshake.RequestToSend;
            _serTwo.Handshake = Handshake.RequestToSend;
            // Act and Assert
            SendAndReceiveBasic();

            _serOne.Handshake = Handshake.None;
            _serTwo.Handshake = Handshake.None;
        }

        [TestMethod]
        public void TestStreams()
        {
            // Arrange
            EnsurePortsOpen();
            _serOne.WriteTimeout = 1000;
            _serOne.ReadTimeout = 1000;
            _serTwo.WriteTimeout = 1000;
            _serTwo.ReadTimeout = 1000;
            EnsurePortEmpty(_serOne);
            EnsurePortEmpty(_serTwo);
            Stream serOneStream = _serOne.BaseStream;
            Stream serTwoStream = _serTwo.BaseStream;
            byte[] toSend = new byte[] { 0x12, 0x34, 0x56, 0x78, 0x90, 0xAB, 0xCD, 0xEF };
            byte[] toReceive = new byte[toSend.Length];
            // Act
            serOneStream.Write(toSend, 0, toSend.Length);
            Thread.Sleep(100);
            serTwoStream.Read(toReceive, 0, toReceive.Length);
            // Assert
            for (int i = 0; i < toSend.Length; i++)
            {
                Assert.Equal(toSend[i], toReceive[i]);
            }
        }

        [TestMethod]
        public void TestEvents()
        {
            // Arrange
            EnsurePortsOpen();
            _serOne.WriteTimeout = 1000;
            _serOne.ReadTimeout = 1000;
            _serTwo.WriteTimeout = 1000;
            _serTwo.ReadTimeout = 1000;
            EnsurePortEmpty(_serOne);
            EnsurePortEmpty(_serTwo); 
            _serTwo.DataReceived += DataReceivedNormalEventTest;
            byte[] toSend = new byte[] { 0x12, 0x34, 0x56, 0x78, 0x90, 0xAB, 0xCD, 0xEF };
            // Act
            _serOne.Write(toSend, 0, toSend.Length);
            // Wait for the data to be received
            Thread.Sleep(200);
            _serTwo.DataReceived -= DataReceivedNormalEventTest;
        }

        private void DataReceivedNormalEventTest(object sender, SerialDataReceivedEventArgs e)
        {
            var ser = (SerialPort)sender;
            Debug.WriteLine($"Event fired, number of bytes ready to read: {ser.BytesToRead}");
            Assert.Equal(8, ser.BytesToRead);
            Assert.True(e.EventType == SerialData.Chars);
        }

        [TestMethod]
        public void TestWatchCharEvents()
        {
            // Arrange
            EnsurePortsOpen();
            _serOne.WriteTimeout = 1000;
            _serOne.ReadTimeout = 1000;
            _serTwo.WriteTimeout = 1000;
            _serTwo.ReadTimeout = 1000;
            EnsurePortEmpty(_serOne);
            EnsurePortEmpty(_serTwo);
            _serTwo.DataReceived += DataReceivedWatchChar;
            _serTwo.WatchChar = '\r';
            string toSendWithWatchChar = "This is a test\r";
            // Act
            _serOne.Write(toSendWithWatchChar);
            Thread.Sleep(200);
            _serTwo.DataReceived -= DataReceivedWatchChar;
        }

        private void DataReceivedWatchChar(object sender, SerialDataReceivedEventArgs e)
        {
            var ser = (SerialPort)sender;
            Debug.WriteLine($"Event fired, number of bytes ready to read: {ser.BytesToRead}");
            Assert.True(e.EventType == SerialData.WatchChar);
        }

        private void SendAndReceiveBasic()
        {
            EnsurePortsOpen();
            EnsurePortEmpty(_serTwo);
            byte[] toSend = new byte[] { 0x12, 0x34, 0x56, 0x78, 0x90, 0xAB, 0xCD, 0xEF };
            byte[] toReceive = new byte[toSend.Length];
            // Act
            _serOne.Write(toSend, 0, toSend.Length);
            // Wait to make sure it will be send
            Thread.Sleep(100);
            _serTwo.Read(toReceive, 0, toReceive.Length);
            // Assert
            for (int i = 0; i < toSend.Length; i++)
            {
                Assert.Equal(toSend[i], toReceive[i]);
            }
        }

        [Cleanup]
        public void CleanPorts()
        {
            EnsurePortsClosed();
            _serOne.Dispose();
            _serTwo.Dispose();
        }

        private void EnsurePortsClosed()
        {
            if (_serOne.IsOpen)
            {
                _serOne.Close();
            }

            if (_serTwo.IsOpen)
            {
                _serTwo.Close();
            }
        }

        private void EnsurePortEmpty(SerialPort port)
        {
            // Ensure nothing to read, if yes, read all
            while (port.BytesToRead > 0)
            {
                port.ReadByte();
            }
        }

        private void EnsurePortsOpen()
        {
            if (!_serOne.IsOpen)
            {
                _serOne.Open();
            }

            if (!_serTwo.IsOpen)
            {
                _serTwo.Open();
            }
        }
    }
}
