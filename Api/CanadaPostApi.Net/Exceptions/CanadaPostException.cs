using System;

namespace CanadaPostApi.Exceptions
{
    public class CanadaPostException : Exception
    {
        public CanadaPostException(string message) : base(message) {}
    }
}
