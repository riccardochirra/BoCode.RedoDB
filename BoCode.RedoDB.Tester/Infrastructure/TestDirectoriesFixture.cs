using System;
using System.IO;

namespace BoCode.RedoDB.Tester.Infrastructure
{
    /// <summary>
    /// used to delete the root of all test directories at the end of 
    /// test collection.
    /// </summary>
    public class TestDirectoriesFixture : IDisposable
    {
        public const string TEST_DIRECTORIES = "Test Directories";

        public void Dispose()
        {
            System.Diagnostics.Trace.Write($"Deleting folder '{TEST_DIRECTORIES}'...");
            //TODO call DeleteTestDirectories causes an IO Exception because the directory is accessed by other processes and can't be delted. 
            //This test suite does not clean up file system yet.
        }

        public void DeleteTestDirectories()
        {
            try
            {
                Directory.Delete(Path.Combine(Environment.CurrentDirectory, TEST_DIRECTORIES), recursive: true);
            }
            catch (System.IO.DirectoryNotFoundException)
            {
                System.Diagnostics.Trace.Write($"Folder {TEST_DIRECTORIES} not deleted because missing!");
            }
        }
    }
}
