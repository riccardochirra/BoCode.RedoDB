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
    /// This class must be used to build a RedoDBEngine. The builder is also responsible for the 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="I"></typeparam>
    public class RedoDBEngineBuilder<T, I> : IWithDataPath
        where T : class
        where I : class
    {
        private IInterceptions _interceptions;
        private readonly Func<T> _creator;
        private ICommandAdapter? _commandAdapter;
        private ISnapshotAdapter<T>? _snapshotAdapter;
        private bool _withNoPersistence;
        private string? _dataPath;
        private IRedoableClock? _redoableClock = null;
        private IRedoableGuid? _redoableGuid = null;
        private bool _compensationActive = false;
        private CompensationManager<T>? _compensationManager;
        private List<Command> _recoveredCommands = new();
        private bool _withCommandlogOnly = false;
        private bool _withJsonAdapters;

        public List<Command> FaultyCommands { get; private set; } = new List<Command>();

        /// <summary>
        /// The parameterless constructor uses and empty InterceptInstructions and the default CommandManagager. 
        /// </summary>
        public RedoDBEngineBuilder(Func<T> creator = null)
        {
            _interceptions = new InterceptionsManager();
            _creator = creator ?? Activator.CreateInstance<T>;
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
        /// restricted mode where only selected methods get logged. This is useful to speed up restore time. You should intercept
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
        /// returns the constructed RedoEngine acting as the Interface you provide in the generic type I. Internally RedoEngine references
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
                CreateNoPersistenceAdapters();
            }
            else
            {
                if (_withJsonAdapters)
                {
                    //use built-in json adapters
                    CreateJsonAdapters();
                }
                else
                {
                    //user injected data adapters
                    EnsureAdapters();
                }
            }
            if (_compensationActive)
            {
                ActivateCompensationManager();
            }
        }

        private void ActivateCompensationManager()
        {
            if (_withCommandlogOnly) throw new RedoDBEngineBuilderException("Compensation requires a snapshot adapter and is not compatible with the option 'WithCommandlogOnly'!");
            _compensationManager = new CompensationManager<T>(_creator);
            _compensationManager.SetSnapshotAdapter(_snapshotAdapter);
        }

        private void CreateNoPersistenceAdapters()
        {
            _commandAdapter = new NoPersitenceCommandAdapter();
            _snapshotAdapter = new NoPersistenceSnapshotAdapter<T>();
        }

        private void CreateJsonAdapters()
        {
            DirectoryInfo path = GetPath();
            _commandAdapter = new JsonCommandAdapter(path, new CommandlogNameProvider());
            if (_withCommandlogOnly)
                _snapshotAdapter = new NoPersistenceSnapshotAdapter<T>();
            else
                _snapshotAdapter = new JsonSnapshotAdapter<T>(path, new SnapshotNameProvider());
        }

        private void EnsureAdapters()
        {
            if (_commandAdapter is null) throw new RedoDBEngineBuilderException("No command adapter was configured, but is required!");
            if (_snapshotAdapter is null && _withCommandlogOnly == false) new RedoDBEngineBuilderException("No snapshot adapter is configured, but is required!");
        }

        private DirectoryInfo GetPath()
        {
            if (_dataPath is null) throw new RedoDBEngineBuilderException(RedoDBEngineBuilderException.MISSING_DATA_PATH);
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
                T newInstance = _creator();
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
            if (_withNoPersistence) return _creator();

            if (_commandAdapter is null) throw new ArgumentNullException(nameof(_commandAdapter));
            if (_snapshotAdapter is null) throw new ArgumentNullException(nameof(_snapshotAdapter));

            T recovered = await _snapshotAdapter.DeserializeAsync() ?? _creator();

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
 
            foreach (Commandlog log in _commandAdapter.RecoveringLogs)
            {
                Redo(recovered, log);
            }
        }

        private T RecoverRedoable()
        {
            if (_withCommandlogOnly || _withNoPersistence) return _creator();

            if (_commandAdapter is null) throw new ArgumentNullException(nameof(_commandAdapter));
            if (_snapshotAdapter is null) throw new ArgumentNullException(nameof(_snapshotAdapter));

            T recovered = _snapshotAdapter.Deserialize() ?? _creator();

            RecoverFromLogs(recovered);

            return recovered;
        }

        private void HandleRedoableDependencies(T recovered)
        {
            if (recovered is IDependsOnRedoableGuid recoveredRedoableGuid)
            {
                _redoableGuid = new RedoableGuid();
                recoveredRedoableGuid.SetRedoableGuid(_redoableGuid);
            }
            if (recovered is IDependsOnRedoableClock recoveredRedoableClock)
            {
                _redoableClock = new RedoableClock();
                recoveredRedoableClock.SetRedoableClock(_redoableClock);
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
        public RedoDBEngineBuilder<T, I> WithJsonAdapters(string dataPath)
        {
            _withJsonAdapters = true;
            _dataPath = dataPath;
            return this;
        }

        public RedoDBEngineBuilder<T, I> WithCommandlogOnly()
        {
            if (_withNoPersistence) throw new RedoDBEngineException("The WithCommandlogOnly option can't be used together with WithNoPersistence!");
            _withCommandlogOnly = true;
            return this;
        }

        public RedoDBEngineBuilder<T, I> WithCommandlogOnly(ICommandAdapter adapter)
        {
            WithCommandlogOnly();
            _commandAdapter = adapter;
            _snapshotAdapter = new NoPersistenceSnapshotAdapter<T>();
            return this;
        }

        void IWithDataPath.WithJsonAdapters(string dataPath)
        {
            _dataPath = dataPath;
        }
    }
}
