namespace BoCode.RedoDB.Persistence
{
    /// <summary>
    /// This interface is implemented by commands adapter or snapshot adapter using a directory to persist data and by the RedoDBEngineBuilder
    /// When called on the Builder, the builder tries to give the data path forward to the command adapter and snapshot adapter.
    /// </summary>
    interface IWithDataPath
    {
        void WithJsonAdapters(string dataPath);
    }
}
