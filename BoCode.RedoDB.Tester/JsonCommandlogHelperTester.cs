using BoCode.RedoDB.Persistence.Commands;
using FluentAssertions;
using System.Linq;
using Xunit;

namespace BoCode.RedoDB.Tester
{

    public class JsonCommandlogHelperTester
    {
        [Fact(DisplayName = "GIVEN a log with two serialized commands " +
            "WHEN I use GetCommandsJson " +
            "THEN two deserializable jsons strings are returned.")]
        public void Test1()
        {
            string commandlog = @"{""CommandType"":""0"",""Name"":""Command1"",""Args"":[""2021-07-13T00:00:00""],""CommandContext"":null}" +
                                @"{""CommandType"":""0"",""Name"":""Command2"",""Args"":[""Riccaro Chirra""],""CommandContext"":null}";
            var jsons = new JsonCommandlogHelper(commandlog).GetCommandJsons();
            jsons.Count().Should().Be(2);
            jsons.First().Should().Be(@"{""CommandType"":""0"",""Name"":""Command1"",""Args"":[""2021-07-13T00:00:00""],""CommandContext"":null}");
            jsons.Last().Should().Be(@"{""CommandType"":""0"",""Name"":""Command2"",""Args"":[""Riccaro Chirra""],""CommandContext"":null}"); ;
        }

        [Fact(DisplayName = "A real command")]
        public void Test2()
        {
            string commandlog = "{\"MethodName\":\"AddContact\",\"Args\":[{\"Name\":\"Alessandro\",\"BirthYear\":0}],\"CommandContext\":{ \"TimeSpamp\":\"2021-07-14T08:53:38.327413+02:00\"}}";

            var jsons = new JsonCommandlogHelper(commandlog).GetCommandJsons();
            jsons.Single().Should().Be("{\"MethodName\":\"AddContact\",\"Args\":[{\"Name\":\"Alessandro\",\"BirthYear\":0}],\"CommandContext\":{ \"TimeSpamp\":\"2021-07-14T08:53:38.327413+02:00\"}}");
        }
    }
}
