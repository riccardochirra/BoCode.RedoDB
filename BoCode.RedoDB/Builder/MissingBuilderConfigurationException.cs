using System;

namespace BoCode.RedoDB.Builder
{
    /// <summary>
    /// This exception must be raised from the IBuilderComponetn.AssertBuildReady method if 
    /// The component is not completely configured. Please providde the RedoDBEngine method to be 
    /// userd to complete configuration. Those methods normally begins with "With".
    /// </summary>
    [Serializable]
    public class MissingBuilderConfigurationException : Exception
    {
        public MissingBuilderConfigurationException() { }
        public MissingBuilderConfigurationException(string message) : base(message) { }
        public MissingBuilderConfigurationException(string message, Exception inner) : base(message, inner) { }

        public const string MISSING_DATA_PATH = "Builder tries to use built in Json adapters for commands and snapshot, but the data path for the files is not configured. Use the builder method WithDataPath before build!";
    }
}
