using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BoCode.RedoDB.System
{
    [global::System.AttributeUsage(global::System.AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
    public sealed class RedoableAttribute : Attribute
    {
        // See the attribute guidelines at 
        //  http://go.microsoft.com/fwlink/?LinkId=85236
        readonly string positionalString;

        // This is a positional argument
        public RedoableAttribute(string positionalString)
        {
            this.positionalString = positionalString;

            global::System.Console.WriteLine("Att code...");

            global::System.Diagnostics.Debug.WriteLine("Attibute code...");
        }

        public string PositionalString
        {
            get { return positionalString; }
        }

        // This is a named argument
        public int NamedInt { get; set; }

    }
}
