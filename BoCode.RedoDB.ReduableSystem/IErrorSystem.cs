namespace BoCode.RedoDB.RedoableSystem
{
    public interface IErrorSystem
    {
        int Value { get; set; }

        void IncreaseValueTo(int valueToReach, int throwExceptionAt);
    }
}
