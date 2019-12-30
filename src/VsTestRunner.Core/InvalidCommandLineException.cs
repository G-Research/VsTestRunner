using System;
using System.Runtime.Serialization;

namespace VsTestRunner.Core
{
    [Serializable]
    internal class InvalidCommandLineException : Exception
    {
        public InvalidCommandLineException()
        {
        }

        public InvalidCommandLineException(string message) : base(message)
        {
        }

        public InvalidCommandLineException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected InvalidCommandLineException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}