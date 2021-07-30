using System;

namespace BoCode.RedoDB
{


    public class RedoDBEngineException : RedoDBException
    {
        public RedoDBEngineException() { }
        public RedoDBEngineException(string message) : base(message) { }
        public RedoDBEngineException(string message, Exception inner) : base(message, inner) { }

    }
}
