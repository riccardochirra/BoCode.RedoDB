using BoCode.RedoDB.Persistence;
using BoCode.RedoDB.Persistence.Commands;
using BoCode.RedoDB.RedoableData;
using System;
using System.Collections.Generic;
using System.Linq;
using BoCode.RedoDB.Persistence.Snapshots;

namespace BoCode.RedoDB.Compensation
{
    /// <summary>
    /// This class support compensation in case of faulty command.
    /// </summary>
    public class CompensationManager<T> where T : class, new()
    {
        List<Command> _faultyCommands = new();

        private ISnapshotAdapter<T>? _snapshotAdapter;
        private IRedoableGuid? _redoableGuid;
        private IRedoableClock? _redoableClock;

        public CompensationManager()
        {
        }

        public IEnumerable<Command> FaultyCommands => _faultyCommands;

        public T Compensate(Command faultyCommand, Commandlog log)
        {
            return Compensate(faultyCommand, log.Commands.ToList());
        }

        public T Compensate(Command faultyCommand, IList<Command> log)
        {
            if (_snapshotAdapter is null) throw new ArgumentNullException(nameof(_snapshotAdapter));
            _faultyCommands.Add(faultyCommand);
            
            T compensated = _snapshotAdapter.GetLastSnapshot() ?? new T();
            
            int i = 0;
            var command = log[0];
            while (command != faultyCommand)
            {
                if (!FaultyCommands.Contains(command))
                {
                    PrepareRedoingData(command);
                    SwitchRedo(compensated, command);
                }

                i++;
                if (i > log.Count - 1) break;
                command = log[i];
            }
            return compensated;
        }

        private void PrepareRedoingData(Command command)
        {
            if (command.CommandContext.TrackedGuids is not null && _redoableGuid is not null)
                _redoableGuid.Redoing(command.CommandContext.TrackedGuids);
            if (command.CommandContext.TrackedTime is not null && _redoableClock is not null)
                _redoableClock.Redoing(command.CommandContext.TrackedTime);
        }

        internal void SetRedoableData(IRedoableGuid? redoableGuid, IRedoableClock? redoableClock)
        {
            _redoableGuid = redoableGuid;
            _redoableClock = redoableClock;
        }

        public static void SwitchRedo(T recovered, Command command)
        {
            switch (command.CommandType)
            {
                case CommandType.Method:
                    RedoInvoke(recovered, command);
                    break;
                case CommandType.Getter:
                    var getter = recovered.GetType().GetProperty(command.MemberName);
                    if (getter is not null)
                        getter.GetGetMethod()?.Invoke(recovered, null);
                    break;
                case CommandType.Setter:
                    RedoSetter(recovered, command);
                    break;
            }
        }

        public static void RedoInvoke(T recovered, Command command)
        {
            var method = recovered.GetType().GetMethod(command.MemberName);
            var parameters = method?.GetParameters();
            if (parameters is not null)
            {
                object?[]? args = new object[parameters.Count()];
                int i = 0;
                foreach (var p in parameters)
                {
                    object? value = null;
                    if (command.Args is not null)
                    {
                        value = command.Args[i];
                    }
                    if (p.ParameterType == typeof(int))
                    {
                        if (command.Args is not null)
                        {
                            Int64 value64 = (Int64)(command.Args.ElementAt(i) ?? 0);
                            value = Int32.Parse(value64.ToString());
                        }
                    }
                    args[i] = value;
                    i++;
                }
                if (method is not null) method.Invoke(recovered, args);
            }
            else
                if (method is not null) method.Invoke(recovered, command.Args);
        }

        private static void RedoSetter(T recovered, Command command)
        {
            var setter = recovered.GetType().GetProperty(command.MemberName);
            if (setter is not null)
            {
                if (setter.PropertyType == typeof(int))
                {
                    Int32 value = 0;
                    if (command.Args is not null)
                    {
                        value = Convert.ToInt32(command.Args.FirstOrDefault());
                    }
                    setter.GetSetMethod()?.Invoke(recovered, new object[] { value });
                }
                else
                    setter.GetSetMethod()?.Invoke(recovered, command.Args);
            }
        }

        internal void SetSnapshotAdapter(ISnapshotAdapter<T> snapshotAdapter) => _snapshotAdapter = snapshotAdapter;
    }
}
