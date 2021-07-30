using System;

namespace BoCode.RedoDB
{
    [global::System.AttributeUsage(global::System.AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class RedoSubdirectoryAttribute : Attribute
    {
        private string _subdirectory;

        // See the attribute guidelines at 
        //  http://go.microsoft.com/fwlink/?LinkId=85236

        /// <summary>
        /// This class level attribute marks your redoable system to be persisted in the specified subdirectory under the datapath.
        /// </summary>
        /// <param name="directoryName">Subdirectory of datapath where you want your redoable system to be persisted. Provide only the directory name, not the fullname</param>
        public RedoSubdirectoryAttribute(string subdirectoryName)
        {
            global::System.Diagnostics.Debug.WriteLine("RedoSubdirectory ctor");

            _subdirectory = subdirectoryName;
        }

        public string Subdirectory => _subdirectory;
    }
}
