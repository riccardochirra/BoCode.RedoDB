using System;

namespace BoCode.RedoDB.RedoableData
{
    public class RedoDBRedoableException : RedoDBException
    {
        public RedoDBRedoableException() { }
        public RedoDBRedoableException(string message) : base(message) { }
        public RedoDBRedoableException(string message, Exception inner) : base(message, inner) { }

    }
}
