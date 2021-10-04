using System;
using System.Runtime.Serialization;

namespace Bnnsoft.Sdk.Signers
{
    [Serializable]
    public class SignHMacException : Exception
    {
        public SignHMacException()
        {
        }

        public SignHMacException(string message) : base(message)
        {
        }

        public SignHMacException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected SignHMacException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}