// Copyright (c) .NET Foundation and Contributors
// See LICENSE file in the project root for full license information.

namespace System.IO.Ports
{
    /// <summary>
    /// Specifies the parity bit for a <see cref="SerialPort"/> object.
    /// </summary>
    public enum Parity
    {
        /// <summary>
        /// No parity check occurs.
        /// </summary>
        None = 0,

        /// <summary>
        /// Sets the parity bit so that the count of bits set is an odd number.
        /// </summary>
        Odd = 1,

        /// <summary>
        /// Sets the parity bit so that the count of bits set is an even number.
        /// </summary>
        Even = 2
    }
}