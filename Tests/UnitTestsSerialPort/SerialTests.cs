using nanoFramework.Hardware.Esp32;
using nanoFramework.TestFramework;
using System;
using System.Diagnostics;
using System.IO.Ports;
using System.Threading;

namespace UnitTestsSerialPort
{
    [TestClass]
    public class SerialTests
    {
        static SerialPort _serOne;
        static SerialPort _serTwo;

        [Setup]
        public void SetupComPorts()
        {
            try
            {
                Debug.WriteLine("Please adjust for your own usage. If you need another hardware, please add the proper nuget and adjust as well");
                Configuration.SetPinFunction(34, DeviceFunction.COM2_RX);
                Configuration.SetPinFunction(35, DeviceFunction.COM2_TX);
                Configuration.SetPinFunction(32, DeviceFunction.COM2_RTS);
                Configuration.SetPinFunction(33, DeviceFunction.COM2_CTS);

                Configuration.SetPinFunction(16, DeviceFunction.COM3_RX);
                Configuration.SetPinFunction(17, DeviceFunction.COM3_TX);
                Configuration.SetPinFunction(27, DeviceFunction.COM3_RTS);
                Configuration.SetPinFunction(14, DeviceFunction.COM3_CTS);

                Debug.WriteLine("You will need to connect:");
                Debug.WriteLine("  COM2 RX  <-> COM3 TX");
                Debug.WriteLine("  COM2 TX  <-> COM3 RX");
                Debug.WriteLine("  COM2 RTS <-> COM3 CTS");
                Debug.WriteLine("  COM2 CTS <-> COM3 RTS");
                _serOne = new SerialPort("COM2");
                _serTwo = new SerialPort("COM3");
                Debug.WriteLine("Devices created, trying to open them");
                _serOne.Open();
                _serTwo.Open();
                Debug.WriteLine("Devices opened, will close them");
                // Wait a bit just to make sure and close them again
                Thread.Sleep(100);
                _serOne.Close();
                _serTwo.Close();
            }
            catch
            {
                Assert.SkipTest("Serial Ports not supported in this platform or not properly configured");
            }
        }

        [TestMethod]
        public void TestMethod1()
        {
        }
    }
}
