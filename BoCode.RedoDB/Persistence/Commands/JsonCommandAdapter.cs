using BoCode.RedoDB.Builder;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BoCode.RedoDB.Persistence.Infrastructure;

namespace BoCode.RedoDB.Persistence.Commands
{
    public class JsonCommandAdapter : ICommandAdapter, IBuilderComponent, IWithDataPath
    {
        private DirectoryInfo _dataPath;
        private bool _withNoPersistence = false;
        private readonly ISnapshotOrLogNameProvider _nameProvider;
        private FileStream? _fileStream = null;
        private string? _lastCommandLog = null;
        private List<Commandlog>? _recoveringLogs;
        private string? _lastSnapshotName;


        public JsonCommandAdapter(DirectoryInfo dataPath, ISnapshotOrLogNameProvider nameProvider)
        {
            _dataPath = dataPath;
            _nameProvider = nameProvider;
        }
        public void WithJsonAdapters(string dataPath)
        {
            _dataPath = new(dataPath);
        }

        public async Task WriteCommandAsync(Command command)
        {
            if (_fileStream is null) NextCommandLog();

            if (_fileStream is not null)
            {
                await using var stream = new MemoryStream();
                var serializer = new JsonSerializer();
                serializer.TypeNameHandling = TypeNameHandling.Objects;
                await using (var sw = new StreamWriter(stream))
                {
                    using (JsonWriter writer = new JsonTextWriter(sw))
                    {
                        serializer.Serialize(writer, command, typeof(Command));
                    }
                }

                //get bytes from stream
                byte[] bytes = stream.GetBuffer();

                EnsureCommandLog();

                _fileStream.SetLength(_fileStream.Position + bytes.Length);
                await _fileStream.WriteAsync(bytes);
                await _fileStream.FlushAsync();
            }
        }

        public void CloseCommandLog()
        {
            if (_fileStream != null)
            {
                _fileStream.Close();
                _fileStream.Dispose();
                _fileStream = null;
            }
        }

        private void EnsureCommandLog()
        {
            if (_fileStream == null)
                NextCommandLog();
        }

        private void NextCommandLog()
        {
            EnsureDataPath();
            string fileName = LastCommandLog == null ? _nameProvider.FirstName : _nameProvider.NewName(new FileInfo(LastCommandLog).Name);
            string fullName = Path.Combine(_dataPath.FullName, fileName);
            _fileStream = new FileInfo(fullName).Open(FileMode.CreateNew, FileAccess.Write, FileShare.Read);
        }

        private void EnsureDataPath()
        {
            if (!Directory.Exists(_dataPath.FullName))
                Directory.CreateDirectory(_dataPath.FullName);
        }

        public void AssertBuildReady()
        {
            if (_withNoPersistence) return;
            if (_dataPath == null)
                throw new MissingBuilderConfigurationException("JsonCommandAdapter needs a data path. Use the builder WithDataPath method do configure it before build.");
        }

        public void NoPersistence()
        {
            _withNoPersistence = true;
        }

        public string? LastCommandLog
        {
            get
            {
                if (_lastCommandLog is null)
                {
                    var files = _dataPath.GetFiles("*.commandlog").OrderBy(x => x.Name).ToList();
                    if (files.Any())
                    {
                        _lastCommandLog = files.Last().FullName;
                    }
                }
                return _lastCommandLog;
            }
        }

        public IEnumerable<Commandlog> RecoveringLogs
        {
            get
            {
                if (_lastSnapshotName == null)
                {
                    throw new RedoDBEngineException("LastSnapshotName not set to the command adapter. Please make sure the LastSnapshtoName property is set before starting using the command adapter.");
                }

                if (_recoveringLogs == null)
                {
                    long snapshotOrdinal = GetLastSnapshotOrdinal();

                    _recoveringLogs = new List<Commandlog>();
                    var files = Directory.GetFiles(_dataPath.FullName, "*.commandlog");
                    var commandlogs = GetFileNames(files).OrderBy(x => x).Where(x => long.Parse(x.Split('.').First()) > snapshotOrdinal);
                    foreach (string commandlog in commandlogs)
                    {
                        Commandlog log = ReadCommandlog(commandlog);
                        _recoveringLogs.Add(log);
                    }
                }
                return _recoveringLogs;
            }
        }

        private long GetLastSnapshotOrdinal()
        {
            long snapshotOrdinal;
            if (!string.IsNullOrEmpty(_lastSnapshotName))
                snapshotOrdinal = long.Parse(_lastSnapshotName.Split('.').First());
            else
                snapshotOrdinal = 0;
            return snapshotOrdinal;
        }

        private IEnumerable<string> GetFileNames(string[] files)
        {
            foreach (string fullname in files)
            {
                yield return new FileInfo(fullname).Name;
            }
        }

        public string? LastSnapshotName { get => _lastSnapshotName; set => _lastSnapshotName = value; }

        private Commandlog ReadCommandlog(string commandlog)
        {
            List<Command> commands = new List<Command>();

            Commandlog log;

            var jsons = GetJsons(commandlog, out log);

            foreach (string json in jsons)
            {
                Command command = DeserializeCommand(json);
                commands.Add(command);
            }

            log.Commands = commands;

            return log;
        }

        private Command DeserializeCommand(string json)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(json);
            using MemoryStream stream = new MemoryStream(bytes);
            var serializer = new JsonSerializer();
            serializer.TypeNameHandling = TypeNameHandling.Objects;
            using StreamReader streamReader = new StreamReader(stream);
            using JsonReader reader = new JsonTextReader(streamReader);
            var result = serializer.Deserialize<Command>(reader);
            if (result is null) throw new RedoDBEngineException("Unexpected null by deserialization of command");
            return result;
        }

        private IEnumerable<string> GetJsons(string commandlog, out Commandlog log)
        {

            using StreamReader reader = new StreamReader(Path.Combine(_dataPath.FullName, commandlog));
            string data = reader.ReadToEnd();
            //string data = File.ReadAllText(Path.Combine(_dataPath.FullName, commandlog));
            log = new(commandlog, data);
            return new JsonCommandlogHelper(data).GetCommandJsons();
        }

        public void Dispose()
        {
            CloseCommandLog();
            System.Diagnostics.Debug.WriteLine("JsonCommandAdapter disposed.");
        }
    }

}
