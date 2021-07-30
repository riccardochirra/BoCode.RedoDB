using Xunit;

namespace BoCode.RedoDB.Tester.Infrastructure
{

    [CollectionDefinition("Test Directory Collection")]
    public class TestDirectoryCollection : ICollectionFixture<TestDirectoriesFixture>
    {
        //This class has no code, and is never created.
        //Its purpose is simply to be the place to apply [CollectionDefinition] and all teh
        //ICollectionFixture<> interfaces.
    }
}
