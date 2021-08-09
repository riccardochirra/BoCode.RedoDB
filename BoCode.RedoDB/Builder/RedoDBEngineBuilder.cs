using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BoCode.RedoDB.Compensation;
using BoCode.RedoDB.Interception;
using BoCode.RedoDB.Persistence;
using BoCode.RedoDB.Persistence.Commands;
using BoCode.RedoDB.Persistence.NoPersistence;
using BoCode.RedoDB.Persistence.Snapshots;
using BoCode.RedoDB.RedoableData;
using ImpromptuInterface;

namespace BoCode.RedoDB.Builder
{
    /// <summary>
    /// If you want to have more control on how the RedoEngine operates, you should use the RedoEngineBuilder to configure it.
    /// The builder let choose what to intercept even if you are not the author of the class you want to become redoable. 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="I"></typeparam>
    public class RedoDBEngineBuilder<T, I> : IWithDataPath
        where T : class, new()
        where I : class
    {
        private IInterceptions _interceptions;
        private ICommandAdapter? _commandAdapter;
        private ISnapshotAdapter<T>? _snapshotAdapter;
        private bool _withNoPersistence;
        private string? _dataPath;
        private IRedoableClock? _redoableClock = null;
        private IRedoableGuid? _redoableGuid = null;
        private bool _compensationActive = false;
        private CompensationManager<T>? _compensationManager;
        private List<Command> _recoveredCommands = new();

        public List<Command> FaultyCommands { get; private set; } = new List<Command>();

        /// <summary>
        /// The parameterless constructor uses and empty InterceptInstructions and the default CommandManagager. 
        /// </summary>
        public RedoDBEngineBuilder()
        {
            _interceptions = new InterceptionsManager();
        }

        public RedoDBEngineBuilder(IInterceptions interceptionManager)
        {
            _interceptions = interceptionManager;
        }

        public RedoDBEngineBuilder<T, I> WithNoPersistence()
        {
            _withNoPersistence = true;
            return this;
        }

        public RedoDBEngineBuilder(ISnapshotAdapter<T> snapshotAdapter, ICommandAdapter commandAdapter) : this()
        {
            _commandAdapter = commandAdapter;
            _snapshotAdapter = snapshotAdapter;
        }

        public RedoDBEngineBuilder(ISnapshotAdapter<T> snapshotAdapter, ICommandAdapter commandAdapter, IInterceptions interceptions) : this(interceptions)
        {
            _commandAdapter = commandAdapter;
            _snapshotAdapter = snapshotAdapter;
        }

        /// <summary>
        /// If you configure compensation on exceptions, the system is automatically restore to the state before executing the command causing an exception. With this option
        /// is not possible to leave invalid state inside the system as a command results in a non execution or complete execution. Incomplete state changes are not possible with
        /// compensation active.
        /// </summary>
        public void WithCompensation()
        {
            _compensationActive = true;
        }

        /// <summary>
        /// By default all methods of a redoable class are intercepted, so that RedoEngine can log the call and redo if state 
        /// of the class must be restored. If you start using the Intercept method in the builder, the RedoEngine switches to a
        /// restricted mode where only selected methods get logged. This is usefull to speed up restore time. You should intercept
        /// all methods that change the state hold by the class. Methods that just retrieve data without changing the state do not need
        /// to be intercepted. An alternative is to mark the method using the RedoableAttribute. If you use the RedoableAttribute, you 
        /// should not use the Intercept method in the builder and let RedoEngine select what to intercept based on the RedoableAttribute.
        /// </summary>
        /// <param name="methodName"></param>
        /// <returns></returns>
        public RedoDBEngineBuilder<T, I> Intercept(string methodName)
        {
            _interceptions.AddInterception(methodName);
            return this;
        }

        public RedoDBEngineBuilder<T, I> InterceptGetter(string name)
        {
            _interceptions.AddGetterInterception(name);
            return this;
        }

        public RedoDBEngineBuilder<T, I> ExcludeMethodsStartingWith(string startingSubstring)
        {
            _interceptions.ExcludeMembersStartingWith(startingSubstring);
            return this;
        }

        /// <summary>
        /// returns the constructed RedoEngine acting as the Interface you provide in the generic type I. Internally RedoEngine refererences
        /// the class T implementing I and calls the methods of T after having logged the call. This allows RedoEngine to restore the state of T 
        /// if needed.
        /// </summary>
        /// <returns></returns>
        public async Task<I> BuildAsync()
        {
            PrepareComponents();
            BuildReadyOrThrowException();
            T recovered = await RecoverRedoableAsync();
            I redoable = CreateEngine(recovered);
            StartEngine(redoable);
            return redoable;
        }

        public I Build()
        {
            PrepareComponents();
            BuildReadyOrThrowException();
            T recovered = RecoverRedoable();
            I redoable = CreateEngine(recovered);
            StartEngine(redoable);
            return redoable;
        }

        private void PrepareComponents()
        {
            if (_withNoPersistence)
            {
                _commandAdapter = new NoPersitenceCommandAdapter();
                _snapshotAdapter = new NoPersistenceSnapshotAdapter<T>();
            }
            else
            {
                DirectoryInfo path = GetPath();
                _commandAdapter = new JsonCommandAdapter(path, new CommandLogNameProvider());
                _snapshotAdapter = new JsonSnapshotAdapter<T>(path, new SnapshotNameProvider());
            }
            if (_compensationActive)
            {
                _compensationManager = new CompensationManager<T>();
                _compensationManager.SetSnapshotAdapter(_snapshotAdapter);
            }
        }

        private DirectoryInfo GetPath()
        {
            if (_dataPath is null) throw new MissingBuilderConfigurationException(MissingBuilderConfigurationException.MISSING_DATA_PATH);
            string? subdirectory = GetSubdirectory();
            if (subdirectory is null)
                return new DirectoryInfo(_dataPath);
            else
                return new DirectoryInfo(Path.Combine(_dataPath, subdirectory));
        }

        private string? GetSubdirectory()
        {
            var attribute = Attribute.GetCustomAttribute(typeof(T),
                typeof(RedoSubdirectoryAttribute)) as RedoSubdirectoryAttribute;
            if (attribute is not null)
                return attribute.Subdirectory;
            else
                return null;
        }

        /// <summary>
        /// Every component is voting build readiness and can put a veto in the build process if the configuration is not complete.
        /// The veto is expressed as a MissingBuilderConfigurationException.
        /// </summary>
        private void BuildReadyOrThrowException()
        {
            if (_commandAdapter is null) throw new ArgumentNullException(nameof(_commandAdapter));
            if (_snapshotAdapter is null) throw new ArgumentNullException(nameof(_snapshotAdapter));

            _interceptions.AssertBuildReady();
            _commandAdapter.AssertBuildReady();
            _snapshotAdapter.AssertBuildReady();
        }

        private I CreateEngine(T recovered)
        {
            if (_commandAdapter is null) throw new RedoDBEngineException("_commandAdapter is null!");
            if (_snapshotAdapter is null) throw new RedoDBEngineException("_snapshotAdapter is null!");
            I redoable;

            if (recovered != null)
            {
                if (recovered is not I) throw new RedoDBEngineException("System T does not implement interface I!");
                redoable = new RedoDBEngine<T>(recovered, _snapshotAdapter, _commandAdapter).ActLike<I>(typeof(IRedoDBEngine<T>), typeof(IRedoEngineInternal<T>));
            }
            else
            {
                T newInstance = new T();
                if (newInstance is not I) throw new RedoDBEngineException("System T does not implement interface I!");
                redoable = new RedoDBEngine<T>(newInstance, _snapshotAdapter, _commandAdapter).ActLike<I>(typeof(IRedoDBEngine<T>), typeof(IRedoEngineInternal<T>));
            }
            return redoable;
        }

        private void StartEngine(I redoable)
        {
            var engine = (IRedoEngineInternal<T>)redoable;
            _interceptions.AddInterceptions(RedoableInspector<I>.GetAllRedoableMethodNames().ToArray());
            engine.SetInterceptionsManager(_interceptions);
            if (_withNoPersistence) engine.DeactivatePersistence();
            if (_compensationActive)
            {
                if (_compensationManager is null) throw new ArgumentNullException(nameof(_compensationManager));
                engine.SetCompensationManager(_compensationManager);
                engine.SetRecoveredFaultyCommands(_compensationManager.FaultyCommands);
            }
            engine.SetRecoveredCommands(_recoveredCommands);

        }

        //get latest snapshot
        //deserialize and return T
        private async Task<T> RecoverRedoableAsync()
        {
            if (_withNoPersistence) return new T();

            if (_commandAdapter is null) throw new ArgumentNullException(nameof(_commandAdapter));
            if (_snapshotAdapter is null) throw new ArgumentNullException(nameof(_snapshotAdapter));

            //recover from snapshot
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            T? recovered = await _snapshotAdapter.DeserializeAsync();
#pragma warning restore CS8602 // Dereference of a possibly null reference.
            if (recovered is null) recovered = new T();

            RecoverFromLogs(recovered);

            return recovered;
        }

        private void RecoverFromLogs(T? recovered)
        {
            HandleRedoableDependencies(recovered);

            _commandAdapter.LastSnapshotName = _snapshotAdapter.LastSnapshot == null ? string.Empty : new FileInfo(_snapshotAdapter.LastSnapshot).Name;

            RedoCommands(recovered);
        }

        private void RedoCommands(T recovered)
        {
            if (_commandAdapter is null) throw new ArgumentNullException(nameof(_commandAdapter));
            //redo commands
            foreach (Commandlog log in _commandAdapter.RecoveringLogs)
            {
                Redo(recovered, log);
            }
        }

        private T RecoverRedoable()
        {
            if (_withNoPersistence) return new T();

            if (_commandAdapter is null) throw new ArgumentNullException(nameof(_commandAdapter));
            if (_snapshotAdapter is null) throw new ArgumentNullException(nameof(_snapshotAdapter));

            //recover from snapshot

            T? recovered = _snapshotAdapter.Deserialize();

            if (recovered is null) recovered = new T();

            RecoverFromLogs(recovered);

            return recovered;
        }

        private void HandleRedoableDependencies(T recovered)
        {
            if (recovered is IDependsOnRedoableGuid)
            {
                _redoableGuid = new RedoableGuid();
                ((IDependsOnRedoableGuid)recovered).SetRedoableGuid(_redoableGuid);
            }
            if (recovered is IDependsOnRedoableClock)
            {
                _redoableClock = new RedoableClock();
                ((IDependsOnRedoableClock)recovered).SetRedoableClock(_redoableClock);
            }
            if (_compensationActive)
            {
                if (_compensationManager is null) throw new ArgumentNullException(nameof(_compensationManager));
                _compensationManager.SetRedoableData(_redoableGuid, _redoableClock);
            }
        }

        private void Redo(T recovered, Commandlog log)
        {
            foreach (Command command in log.Commands)
            {
                PrepareRedoingData(command);
                try
                {
                    CompensationManager<T>.SwitchRedo(recovered, command);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Exception caught while recovering system of type {recovered.GetType().Name}. \nException is of type {ex.GetType().Name}. \nThe exception message is {ex.Message}");
                }
                finally
                {
                    if (_compensationActive)
                    {
                        recovered = Compensate(command, log);
                    }
                }
                _recoveredCommands.Add(command);
            }

        }

        private T Compensate(Command faultyCommand, Commandlog log)
        {
            if (_compensationManager is null) throw new ArgumentNullException(nameof(_compensationManager));
            return _compensationManager.Compensate(faultyCommand, log);
        }


        private void PrepareRedoingData(Command command)
        {
            if (command.CommandContext.TrackedGuids is not null && _redoableGuid is not null)
                _redoableGuid.Redoing(command.CommandContext.TrackedGuids);
            if (command.CommandContext.TrackedTime is not null && _redoableClock is not null)
                _redoableClock.Redoing(command.CommandContext.TrackedTime);
        }

        /// <summary>
        /// This method of the builder configures RedoDb to use built-in json adapters persisting commands and snapshot in the given directory (data path).
        /// </summary>
        /// <param name="dataPath"></param>
        public void WithJsonAdapters(string dataPath)
        {
            _dataPath = dataPath;
        }
    }
}
