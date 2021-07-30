using BoCode.RedoDB.Persistence.Commands;
using BoCode.RedoDB.RedoableSystem;
using BoCode.RedoDB.Tester.Infrastructure;
using FluentAssertions;
using Newtonsoft.Json;
using System;
using System.IO;
using Xunit;

namespace BoCode.RedoDB.Tester
{
    public class JsonCommandSerializationTester : TesterWithDataPath
    {
        [Fact(DisplayName = "Serialize and deserialize command and args types are preserved.")]
        public void Test1()
        {
            //ARRANGE
            string dataPath = NewDataPath;
            Command command = new Command(CommandType.Method, "AddContact", new object[] { new Contact() { Name = "Riccardo" } }, new CommandContext(DateTime.Now, null, null));

            //ACT serialize and deserialize
            var serializer = new JsonSerializer();
            serializer.TypeNameHandling = TypeNameHandling.Objects;

            using (var sw = new StreamWriter(dataPath))
            using (JsonWriter writer = new JsonTextWriter(sw))
            {
                serializer.Serialize(writer, command, typeof(Command));
            }

            Command recovered;
            using (JsonReader reader = new JsonTextReader(new StreamReader(dataPath)))
            {
                recovered = serializer.Deserialize<Command>(reader);
            }

            //ASSERT
            recovered.Args[0].Should().BeOfType<Contact>();
        }
    }
}
