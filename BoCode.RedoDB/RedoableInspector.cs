using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace BoCode.RedoDB
{
    /// <summary>
    /// This class can extract method names of methods decorated with the RedoableAttribute.
    /// </summary>
    public static class RedoableInspector<I>
    {
        public static IEnumerable<string> GetAllRedoableMethodNames()
        {
            List<string> redoableMethodNames = new List<string>();

            Type type = typeof(I);
            var methodInfos = type.GetMethods();

            foreach (var methodInfo in methodInfos)
            {
                if (HasRedoableAttribute(methodInfo))
                {
                    redoableMethodNames.Add(methodInfo.Name);
                }
            }
            return redoableMethodNames;
        }

        private static bool HasRedoableAttribute(MethodInfo methodInfo)
        {
            var customAttributes = methodInfo.GetCustomAttributes(typeof(RedoableAttribute));
            if (customAttributes != null &&
                customAttributes.Any(x => x is RedoableAttribute)) return true;
            return false;
        }
    }
}
