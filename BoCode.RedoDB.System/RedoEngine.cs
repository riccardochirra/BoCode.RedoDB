using System;
using System.Dynamic;

namespace BoCode.RedoDB.System
{
    /// <summary>
    /// RedoEngine is the principal class (entry point) of the RedoDB system.
    /// RedoEngine is responsible to intercept Redoable methods of T, write commandlogs and 
    /// deserialize T from the snapshot reapplying commandlogs as needed to restore state of T
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class RedoEngine<T> : DynamicObject where T : class, new()
    {
        private readonly T _redoableObject;

        public RedoEngine(T redoableObject)
        {
            _redoableObject = redoableObject;
        }

        public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
        {
            try
            {
                DebugWrite($"Invoking {_redoableObject.GetType().Name}.{binder.Name} with arguments {string.Join(',', args)}");
                result = null;
                return true;
            }
            catch (Exception ex)
            {
                DebugWrite($"Invoking {_redoableObject.GetType().Name}.{binder.Name} with arguments {string.Join(',', args)}");
                result = null;
                return false;
            }
        }

        private void DebugWrite(string message)
        {
            global::System.Diagnostics.Debug.Write(message);
        }
    }
}
