using System;
using System.Collections.Generic;
using System.Text;

namespace Toosame.RaspberryIO.Peripherals.Exceptions
{
    public class DeviceLibInitException : Exception
    {
        public DeviceLibInitException(string message) : base(message)
        {
        }

        public DeviceLibInitException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
