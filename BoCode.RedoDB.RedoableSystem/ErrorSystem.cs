using System;

namespace BoCode.RedoDB.RedoableSystem
{
    /// <summary>
    /// This system is designed to test how RedoDB engine handles errors thrown while executing commands.
    /// </summary>
    public class ErrorSystem : IErrorSystem
    {
        public int Value { get; set; } = 0;
        public void IncreaseValueTo(int valueToReach, int throwExceptionAt)
        {
            while (Value < valueToReach)
            {
                Value = Value + 1;
                if (Value == throwExceptionAt) throw new ApplicationException($"Exception thrown at {Value}.");
            }
        }
    }
}
