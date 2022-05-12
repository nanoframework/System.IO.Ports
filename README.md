[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=nanoframework_System.IO.Ports&metric=alert_status)](https://sonarcloud.io/dashboard?id=nanoframework_System.IO.Ports) [![Reliability Rating](https://sonarcloud.io/api/project_badges/measure?project=nanoframework_System.IO.Ports&metric=reliability_rating)](https://sonarcloud.io/dashboard?id=nanoframework_System.IO.Ports) [![License](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE) [![NuGet](https://img.shields.io/nuget/dt/nanoFramework.System.IO.Ports.svg?label=NuGet&style=flat&logo=nuget)](https://www.nuget.org/packages/nanoFramework.System.IO.Ports/) [![#yourfirstpr](https://img.shields.io/badge/first--timers--only-friendly-blue.svg)](https://github.com/nanoframework/Home/blob/main/CONTRIBUTING.md) [![Discord](https://img.shields.io/discord/478725473862549535.svg?logo=discord&logoColor=white&label=Discord&color=7289DA)](https://discord.gg/gCyBu8T)

![nanoFramework logo](https://raw.githubusercontent.com/nanoframework/Home/main/resources/logo/nanoFramework-repo-logo.png)

-----

# Welcome to the .NET **nanoFramework** System.IO.Ports Library repository

## Build status

| Component | Build Status | NuGet Package |
|:-|---|---|
| System.IO.Ports | [![Build Status](https://dev.azure.com/nanoframework/System.IO.Ports/_apis/build/status/System.IO.Ports?repoName=nanoframework%2FSystem.IO.Ports&branchName=main)](https://dev.azure.com/nanoframework/System.IO.Ports/_build/latest?definitionId=74&repoName=nanoframework%2FSystem.IO.Ports&branchName=main) | [![NuGet](https://img.shields.io/nuget/v/nanoFramework.System.IO.Ports.svg?label=NuGet&style=flat&logo=nuget)](https://www.nuget.org/packages/nanoFramework.System.IO.Ports/) |

## Usage

You will find detailed examples in the [Tests](./Tests/UnitTestsSerialPort).

### Creating the SerialPort

You can create the `SerialPort` like this:

```csharp
var port = new SerialPort("COM2");
```

Note that the port name **must** be `COMx` where x is a number. 

The `GetPortNames` method will give you a list of available ports:

```csharp
var ports = SerialPort.GetPortNames();
```

You can also directly specify the baud rate and other elements in the constructor:

```csharp
var port = new SerialPort("COM2", 115200);
```

Each property can be adjusted, including when the port is open. Be aware that this can generate hazardous behaviors. It is always recommended to change the properties while the port is closed.

**Important**: You should setup a timeout for the read and write operations. If you have none, read or a write periods may cause theads to be locked for indefinite periods.

```csharp
port.WriteTimeout = 1000;
port.ReadTimeout = 1000;
```

Note: some MCU do not support Handshake or specific bit parity even if you can set them up in the constructor.

### Opening and Closing the port

The `SerialPort` can only operate once open and will finish the operations when closed. When disposed, the `SerialPort` will perform the close operation regardless of any ongoing receive or transmit operations.

```csharp
var port = new SerialPort("COM2");
port.Open();
// Do a lot of things here, write, read
port.Close();
```

### Read and Write

You have multiple functions to read and write, some are byte related, others string related. 
Note that string functions will use UTF8 `Encoding` charset.

#### Sending and receiving bytes

Example of sending and reading byte arrays:

```csharp
byte[] toSend = new byte[] { 0x42, 0xAA, 0x11, 0x00 };
byte[] toReceive = new byte[50];
// this will send the 4 bytes:
port.Write(toSend, 0, toSend.Length);
// This will only send the bytes AA and 11:
port.Write(toSend, 1, 2);
// This will check then number of available bytes to read
var numBytesToRead = port.BytesToRead;
// This will read 50 characters:
port.Read(toReceive, 0, toReceive.Length);
// this will read 10 characters and place them at the offset position 3:
port.Read(toReceive, 3, 10);
// Note: in case of time out while reading or writing, you will receive a TimeoutException
// And you can as well read a single byte:
byte oneByte = port.ReadByte();
```

#### Sending and receiving string

Example:

```csharp
string toSend = "I ❤ nanoFramework";
port.WriteLine(toSend);
// this will send the string encoded finishing by a new line, by default `\n`
// You can change the new line to be anything:
port.NewLine = "❤❤";
// Now it will send 2 hearts as the line ending `WriteLine` and will use 2 hearts as the terminator for `ReadLine`.
// You can change it back to the `\n` default at anytime:
port.NewLine = SerialPort.DefaultNewLine; // default is "\n"
// This will read the existing buffer:
string existingString = port.ReadExisting();
// Note that if it can't properly convert the bytes to a string, you'll get an exception
// This will read a full line, it has to be terminated by the NewLine string.
// If nothing is found ending by the NewLine in the ReadTimeout time frame, a TimeoutException will be raised.
string aFullLine = port.ReadLine();
```

### Events

#### Character

SerialPort supports events when characters are received.

```csharp
    // Subscribe to the event
    port.DataReceived += DataReceivedNormalEvent;

    // When you're done, you can as well unsubscribe
    port.DataReceived -= DataReceivedNormalEvent;

private void DataReceivedNormalEvent(object sender, SerialDataReceivedEventArgs e)
{
    var ser = (SerialPort)sender;
    // Now you can check how many characters are available, read a line for example
    var numBytesToRead = port.BytesToRead;
    string aFullLine = ser.ReadLine();
}
```

#### WatchChar


.NET nanoFramework has a custom API event to watch for a specific character if present during the transmission.

```csharp
    port.WatchChar = '\r';
    // Subscribe to the event
    port.DataReceived += DataReceivedNormalEvent;

private void DataReceivedNormalEvent(object sender, SerialDataReceivedEventArgs e)
{
    if (e.EventType == SerialData.WatchChar)
    {
        // The specified character was detected when reading from the serialport.
    }
}
```

## Feedback and documentation

For documentation, providing feedback, issues and finding out how to contribute please refer to the [Home repo](https://github.com/nanoframework/Home).

Join our Discord community [here](https://discord.gg/gCyBu8T).

## Credits

The list of contributors to this project can be found at [CONTRIBUTORS](https://github.com/nanoframework/Home/blob/main/CONTRIBUTORS.md).

## License

The **nanoFramework** Class Libraries are licensed under the [MIT license](LICENSE.md).

## Code of Conduct

This project has adopted the code of conduct defined by the Contributor Covenant to clarify expected behaviour in our community.
For more information see the [.NET Foundation Code of Conduct](https://dotnetfoundation.org/code-of-conduct).

### .NET Foundation

This project is supported by the [.NET Foundation](https://dotnetfoundation.org).
