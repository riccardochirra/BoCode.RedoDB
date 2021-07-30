using System;

namespace BoCode.RedoDB
{
    [global::System.AttributeUsage(global::System.AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public sealed class RedoableAttribute : Attribute
    {
        // See the attribute guidelines at 
        //  http://go.microsoft.com/fwlink/?LinkId=85236

        public RedoableAttribute()
        {
            global::System.Diagnostics.Debug.WriteLine("RedoableAttribute ctor");
        }
    }
}
