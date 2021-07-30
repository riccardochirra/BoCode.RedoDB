using BoCode.RedoDB.Builder;
using Newtonsoft.Json;
using System.IO;
using System.Linq;
using System.Threading.Tasks;


namespace BoCode.RedoDB.Persistence.Snapshots
{
    /// <summary>
    /// This is the default snapshot persistence adapter provided by RedoDB.
    /// if there is no snapshot to deserialize from, deserialization returns default(T);
    /// </summary>
    public class JsonSnapshotAdapter<T> : ISnapshotAdapter<T>, IBuilderComponent, IWithDataPath
    {
        private DirectoryInfo _dataPath;
        private bool _withNoPersistence = false;
        private readonly ISnapshotOrLogNameProvider _nameProvider;
        private MemoryStream? _lastSnapshot = null;

        public JsonSnapshotAdapter(DirectoryInfo dataPath, ISnapshotOrLogNameProvider nameProvider)
        {
            _dataPath = dataPath;
            _nameProvider = nameProvider;
        }


        public async Task<T?> DeserializeAsync()
        {
            return await Task.Run(() => Deserialize());
        }


        public T? Deserialize()
        {
            var lastSnapshot = LastSnapshot;
            T? redoable = default(T);

            if (lastSnapshot is not null)
            {
                //read the file into the memorystream
                MemoryStream memoryStream = new();

                using Stream input = File.OpenRead(lastSnapshot);
                input.CopyTo(memoryStream);
                memoryStream.Position = 0;

                //deserialize from memory stream

                var serializer = new JsonSerializer();
                serializer.TypeNameHandling = TypeNameHandling.Objects;
                using var streamReader = new StreamReader(memoryStream);
                using JsonReader reader = new JsonTextReader(streamReader);
                redoable = serializer.Deserialize<T>(reader);

                //save the stream to be used for compensation.
                _lastSnapshot = new MemoryStream(memoryStream.ToArray());

            }
            return redoable;
        }

        public T? GetLastSnapshot()
        {
            if (_lastSnapshot is not null)
            {
                T? redoable = default(T);
                _lastSnapshot.Seek(0, SeekOrigin.Begin);
                using var streamReader = new StreamReader(_lastSnapshot);
                using JsonReader reader = new JsonTextReader(streamReader);

                var serializer = new JsonSerializer();
                serializer.TypeNameHandling = TypeNameHandling.Objects;
                redoable = serializer.Deserialize<T>(reader);
                _lastSnapshot = new MemoryStream(_lastSnapshot.ToArray());
                return redoable;
            }
            else
                return default(T);
        }

        public string NewSnapshot
        {
            get
            {
                string? lastSnapshot = LastSnapshot;
                string? lastCommandlog = LastCommandlog;
                return GetSnapshotName(lastSnapshot, lastCommandlog);
            }
        }

        /// <summary>
        /// This method ensures that the new snapshot has always the ordinal of the last commandlog.
        /// If there are no commandlogs, it returns the first name if no snapshot was found, otherwise
        /// the next snapshot name based on the ordinal of the last snapshot.
        /// </summary>
        /// <param name="lastSnapshot"></param>
        /// <param name="lastCommandlog"></param>
        /// <returns></returns>
        private string GetSnapshotName(string? lastSnapshot, string? lastCommandlog)
        {
            long lastSnapshotOrdinal = 0;
            if (lastSnapshot is not null) lastSnapshotOrdinal = long.Parse(new FileInfo(lastSnapshot).Name.Split('.').First());
            long lastCommandlogOrdinal = 0;
            if (lastCommandlog is not null) lastCommandlogOrdinal = long.Parse(new FileInfo(lastCommandlog).Name.Split('.').First());

            if (lastCommandlogOrdinal > lastSnapshotOrdinal)
            {
                return _nameProvider.NewName(lastCommandlogOrdinal - 1);
            }
            else
            {
                if (lastSnapshotOrdinal == 0) return _nameProvider.FirstName;
                return _nameProvider.NewName(lastSnapshotOrdinal);
            }
        }

        /// <summary>
        /// Returns null if no commandlog can be found, otherwise it returns the one with the highest ordinal.
        /// </summary>
        private string? LastCommandlog
        {
            get
            {
                EnsureDataPath();
                IOrderedEnumerable<FileInfo> files = _dataPath.GetFiles("*.commandlog").ToList().OrderBy(x => x.Name);
                if (files.Any())
                {
                    var lastCommandlog = files.Last();
                    return lastCommandlog.FullName;
                }
                return null;
            }
        }

        public void Serialize(T redoable)
        {
            EnsureDataPath();
            WriteSnapshot(redoable);
        }

        private void EnsureDataPath()
        {
            if (!Directory.Exists(_dataPath.FullName))
                Directory.CreateDirectory(_dataPath.FullName);
        }

        public void WriteSnapshot(T redoable)
        {
            //seriazize to memory stream first, and save it in _lastSnapshot for compensation.
            MemoryStream stream = SerializeToMemoryStream(redoable);


            //now write stream to disk
            FileInfo fileInfo = new FileInfo(Path.Combine(_dataPath.FullName, NewSnapshot));

            //You have to rewind the MemoryStream before copying
            stream.Seek(0, SeekOrigin.Begin);

            using (FileStream fs = new FileStream(fileInfo.FullName, FileMode.OpenOrCreate))
            {
                stream.CopyTo(fs);
                fs.Flush();
            }
        }

        private MemoryStream SerializeToMemoryStream(T redoable)
        {
            var stream = new MemoryStream();
            var sw = new StreamWriter(stream);
            var jsonWriter = new JsonTextWriter(sw);

            var s = new JsonSerializer();
            s.TypeNameHandling = TypeNameHandling.Objects;
            s.Serialize(jsonWriter, redoable);
            jsonWriter.Flush();
            sw.Flush();
            stream.Seek(0, SeekOrigin.Begin);
            _lastSnapshot = stream;
            return stream;
        }

        public void AssertBuildReady()
        {
            if (_withNoPersistence) return;
            if (_dataPath == null)
                throw new MissingBuilderConfigurationException("JsonSnapshotAdapter needs a data path. Use the builder WithDataPath method do configure it before build.");
        }

        public void WithJsonAdapters(string dataPath)
        {
            _dataPath = new(dataPath);
        }

        public void NoPersistence()
        {
            _withNoPersistence = true;
        }

        public void Dispose()
        {
            System.Diagnostics.Debug.WriteLine("JsonSnapshotAdapter disposed.");
        }

        /// <summary>
        /// returns null if no snapshot can be found, 
        /// otherwise returns the snapshot fullname with the highest ordinal
        /// </summary>
        public string? LastSnapshot
        {
            get
            {
                EnsureDataPath();
                IOrderedEnumerable<FileInfo> files = _dataPath.GetFiles("*.snapshot").ToList().OrderBy(x => x.Name);
                if (files.Any())
                {
                    var lastSnapshot = files.Last();
                    return lastSnapshot.FullName;
                }
                return null;
            }
        }
    }
}
