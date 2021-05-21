//
// Copyright (c) .NET Foundation and Contributors
// See LICENSE file in the project root for full license information.
//

namespace System.IO.Ports
{
	/// <summary>
	/// Defines values for hardware and software mode of operation.
	/// </summary>
	public enum SerialMode
	{
		/// <summary>
		/// Normal Serial mode with handshake define by SerialHandshake.
		/// </summary>
		Normal,
		/// <summary>
		/// Used for Half duplex RS485 mode. 
		/// Puts the port in to half duplex RS485 mode where the RTS is raised while the port is sending data. Once data is completely sent the RTS signal is lowered.
		/// </summary>
		RS485
	}
}
