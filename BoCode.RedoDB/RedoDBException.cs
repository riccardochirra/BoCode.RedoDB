using System;

namespace BoCode.RedoDB
{
    public class RedoDBException : Exception
    {
        public RedoDBException() { }
        public RedoDBException(string message) : base(message) { }
        public RedoDBException(string message, Exception inner) : base(message, inner) { }

    }
}
