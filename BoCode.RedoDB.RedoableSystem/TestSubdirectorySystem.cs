using System;

namespace BoCode.RedoDB.RedoableSystem
{
    public interface ITestSubdirectorySystem
    {
        public void DoSomething();
    }

    [RedoSubdirectory("Subdirectory")]
    [Serializable]
    public class TestSubdirectorySystem : ITestSubdirectorySystem
    {
        public void DoSomething()
        {
            System.Diagnostics.Debug.WriteLine("Doing something from test.");
        }
    }
}
