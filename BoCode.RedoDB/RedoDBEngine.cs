using BoCode.RedoDB.Compensation;
using BoCode.RedoDB.Persistence;
using BoCode.RedoDB.Persistence.Commands;
using BoCode.RedoDB.RedoableData;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using BoCode.RedoDB.Interception;
using BoCode.RedoDB.Persistence.Snapshots;

namespace BoCode.RedoDB
{
    /// <summary>
    /// RedoEngine is the principal class (entry point) of the RedoDB system.
    /// RedoEngine is responsible to intercept Redoable methods of T, write commandlogs and 
    /// deserialize T from the snapshot reapplying commandlogs as needed to restore state of T
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class RedoDBEngine<T> : DynamicObject, IRedoDBEngine<T>, IRedoEngineInternal<T> where T : class
    {
        private T _redoableObject;
        private IInterceptions _interceptions;
        private ICommandsManager<T> _commands;
        private ISnapshotManager<T> _snapshotManager;
        private bool _noPersistence;
        private IRedoableGuid _redoableGuid;
        private IRedoableClock _redoableClock;
        private object _lock = new object();
        private bool _compensationActive;
        private CompensationManager<T> _compensationManager;

        public RedoDBEngine(T redoableObject, ISnapshotAdapter<T> snapshotAdapter, ICommandAdapter commandAdapter)
        {
            _redoableObject = SetRedoable(redoableObject);
            _interceptions = new InterceptionsManager();
            _commands = new CommandsManager<T>(commandAdapter);
            _snapshotManager = new SnapshotManager<T>(() => _redoableObject, snapshotAdapter);
        }

        private void HandleRedoableDependencies(T redoableObject)
        {
            if (redoableObject is IDependsOnRedoableClock)
            {
                _redoableClock = new RedoableClock();
                ((IDependsOnRedoableClock)redoableObject).SetRedoableClock(_redoableClock);
            }
            if (redoableObject is IDependsOnRedoableGuid)
            {
                _redoableGuid = new RedoableGuid();
                ((IDependsOnRedoableGuid)redoableObject).SetRedoableGuid(_redoableGuid);
            }
        }

        public ICommandsManager<T> Commands { get => _commands; }
        public IInterceptions Instructions { get => _interceptions; }

        public RedoDBEngine<T> Engine => this;

        public void SetCommandsManager(ICommandsManager<T> commandManager)
        {
            _commands = commandManager;
        }

        public void SetInterceptionsManager(IInterceptions interceptionsManager)
        {
            _interceptions = interceptionsManager;
        }

        public void TakeSnapshot()
        {
            if (_noPersistence) throw new RedoDBEngineException("This engine has been configured to intercept method calls only.No persistence is deactivated.TakeSnapshot can't be used.");
            _commands.CloseCommandlog();
            _snapshotManager.TakeSnapshot();
        }

        public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
        {
            try
            {
                result = null;

                if (_interceptions.CanIntercept(binder.Name))
                {
                    return InterceptAndInvoke(binder, args, ref result);
                }
                else
                {
                    return JustInvoke(binder, args, ref result);
                }
            }
            catch (TargetInvocationException ex)
            {
                if (ex.InnerException != null)
                {
                    DebugWrite(ex.InnerException.ToString());
                    throw ex.InnerException;
                }
                throw;
            }
            finally
            {
                if (_compensationActive)
                {
                    SetRedoable(Compensate(_commands.Log.Last(), _commands.Log.ToList()));
                }
            }
        }

        private T SetRedoable(T newRedoable)
        {
            _redoableObject = newRedoable;
            HandleRedoableDependencies(_redoableObject);
            return _redoableObject;
        }

        private T Compensate(Command faultyCommand, IList<Command> log)
        {
            if (_compensationManager is null) throw new ArgumentNullException(nameof(_compensationManager));
            return _compensationManager.Compensate(faultyCommand, log);
        }


        private bool JustInvoke(InvokeMemberBinder binder, object[] args, ref object result)
        {
            var methodInfo = _redoableObject.GetType().GetMethod(binder.Name);

            if (methodInfo is null) return false;

            result = methodInfo.Invoke(_redoableObject, args);

            return true;
        }

        private bool InterceptAndInvoke(InvokeMemberBinder binder, object[] args, ref object result)
        {
            var methodInfo = _redoableObject.GetType().GetMethod(binder.Name);

            if (methodInfo is null) return false;

            lock (_lock)
            {
                try
                {
                    result = methodInfo.Invoke(_redoableObject, args);
                }
                catch { throw; }
                finally
                {
                    (List<Guid> guids, List<DateTime> dateTimes) = GetTrackedData();

                    Command command = new Command(CommandType.Method, binder.Name, args, new CommandContext(DateTime.Now, guids, dateTimes));

                    _commands.AddCommand(command);
                }
            }

            return true;
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            try
            {
                result = null;

                if (_interceptions.CanInterceptGetter(binder.Name))
                {
                    return InterceptAndGet(binder, ref result);
                }
                else
                {
                    return JustGet(binder, ref result);
                }
            }
            catch (TargetInvocationException ex)
            {
                if (ex.InnerException != null)
                {
                    DebugWrite(ex.InnerException.ToString());
                    throw ex.InnerException;
                }
                throw;
            }
        }

        private bool JustGet(GetMemberBinder binder, ref object result)
        {
            var propertyInfo = _redoableObject.GetType().GetProperty(binder.Name);

            if (propertyInfo is null) return false;

            result = propertyInfo.GetValue(_redoableObject, null);

            return true;

        }

        private bool InterceptAndGet(GetMemberBinder binder, ref object result)
        {
            var propertyInfo = _redoableObject.GetType().GetProperty(binder.Name);

            if (propertyInfo is null) return false;

            lock (_lock)
            {
                try
                {
                    result = propertyInfo.GetValue(_redoableObject);
                }
                catch { throw; }
                finally
                {
                    (List<Guid> guids, List<DateTime> dateTimes) = GetTrackedData();

                    Command command = new Command(CommandType.Getter, binder.Name, null, new CommandContext(DateTime.Now, guids, dateTimes));

                    _commands.AddCommand(command);
                }
            }

            return true;
        }

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            try
            {
                if (_interceptions.CanIntercept(binder.Name))
                {
                    return InterceptAndSet(binder, value);
                }
                else
                {
                    return JustSet(binder, value);
                }
            }
            catch (TargetInvocationException ex)
            {
                if (ex.InnerException != null)
                {
                    DebugWrite(ex.InnerException.ToString());
                    throw ex.InnerException;
                }
                throw;
            }
        }

        private bool JustSet(SetMemberBinder binder, object value)
        {
            var propertyInfo = _redoableObject.GetType().GetProperty(binder.Name);

            if (propertyInfo is null) return false;

            propertyInfo.SetValue(_redoableObject, value, null);

            return true;
        }

        private bool InterceptAndSet(SetMemberBinder binder, object value)
        {
            var propertyInfo = _redoableObject.GetType().GetProperty(binder.Name);

            if (propertyInfo is null) return false;

            lock (_lock)
            {
                try
                {
                    propertyInfo.SetValue(_redoableObject, value, null);
                }
                catch { throw; }
                finally
                {
                    (List<Guid> guids, List<DateTime> dateTimes) = GetTrackedData();

                    Command command = new Command(CommandType.Setter, binder.Name, new object[] { value }, new CommandContext(DateTime.Now, guids, dateTimes));

                    _commands.AddCommand(command);
                }
            }

            return true;
        }

        private (List<Guid> guids, List<DateTime> dateTimes) GetTrackedData()
        {
            var result = (_redoableGuid?.Tracked.ToList(), _redoableClock?.Tracked.ToList());
            _redoableGuid?.ClearTracking();
            _redoableClock?.ClearTracking();
            return result;
        }

        public static IRedoDBEngine<T> GetEngine<I>(I redoable)
        {
            if (redoable is IRedoDBEngine<T>) return (IRedoDBEngine<T>)redoable;

            string typeofTname = typeof(T).Name;
            string typeofIname = typeof(I).Name;

            throw new RedoDBEngineException($"Something is wrong with redoable system. Cant be casted to IRedoDBEngine<{typeofTname}>! Are you sure your system implements the system interface {typeofIname}?");
        }


        private void DebugWrite(string message)
        {
            global::System.Diagnostics.Debug.Write(message);
        }

        public void DeactivatePersistence()
        {
            _noPersistence = true;
        }

        public void Dispose()
        {
            _commands.Dispose();
            _snapshotManager.Dispose();

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            _commands = null;
            _snapshotManager = null;
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.        
        }

        public void SetCompensationManager(CompensationManager<T> compensationManager)
        {
            _compensationActive = true;
            _compensationManager = compensationManager;
            _commands.SetCompensationManager(_compensationManager);
        }

        public void SetRecoveredCommands(List<Command> recoveredCommands)
        {
            _commands.RecoveringLog = recoveredCommands;
        }

        public void SetRecoveredFaultyCommands(IEnumerable<Command> faultyCommands)
        {
            _commands.RecoveringLogFaultyCommands = faultyCommands;
        }
    }
}
