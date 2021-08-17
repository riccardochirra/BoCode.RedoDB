using BoCode.RedoDB.RedoableSystem;
using BoCode.RedoDB.Tester.Infrastructure;
using FluentAssertions;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BoCode.RedoDB.Builder;
using Xunit;

namespace BoCode.RedoDB.Tester
{
    [Collection("Test Directory Collection")]
    public class RedoDBEngineTester : TesterWithDataPath
    {
        [Fact(DisplayName = "GIVEN a redoable WHEN a snapshot is taken THEN a snapshot file is found in the data path folder.")]
        public async Task Test1()
        {
            //ARRANGE
            string dataPath = NewDataPath;
            RedoDBEngineBuilder<ContactsSystem, IContactsSystem> builder = new();
            builder.WithJsonAdapters(dataPath);
            IContactsSystem contacts = await builder.BuildAsync();

            contacts.AddContact(new() { Name = "Alessandro" });

            //ACT
            RedoDBEngine<ContactsSystem>.GetEngine(contacts).TakeSnapshot();

            //ASSERT
            new DirectoryInfo(dataPath).GetFiles("*.snapshot").First().Name.Should().Be("00000000000000000001.snapshot");
        }

        [Fact(DisplayName = "TakeSnapshot should not get intercepted. Directly after snapshot the commandlog is empty.")]
        public async Task Test2()
        {
            //ARRANGE
            string dataPath = NewDataPath;
            RedoDBEngineBuilder<ContactsSystem, IContactsSystem> builder = new();
            builder.WithJsonAdapters(dataPath);
            IContactsSystem contacts = await builder.BuildAsync();

            contacts.AddContact(new() { Name = "Alessandro" });

            //ACT
            var engine = RedoDBEngine<ContactsSystem>.GetEngine(contacts);
            engine.TakeSnapshot();

            //ASSERT
            engine.Commands.Log.Count().Should().Be(0);
        }

        [Fact(DisplayName = "Can recover from commandlog")]
        public async Task Test3()
        {
            //ARRANGE
            string dataPath = NewDataPath;

            RedoDBEngineBuilder<ContactsSystem, IContactsSystem> builder = new();
            builder.WithJsonAdapters(dataPath);
            IContactsSystem contacts = await builder.BuildAsync();
            using (var engine = RedoDBEngine<ContactsSystem>.GetEngine(contacts))
            {
                contacts.AddContact(new() { Name = "Alessandro" });
            }
            //ACT creation of a second instance based on the snapshot of the first one.
            //    system must recover from commandlog.
            RedoDBEngineBuilder<ContactsSystem, IContactsSystem> builder2 = new();
            builder2.WithJsonAdapters(dataPath);
            IContactsSystem contacts2 = await builder2.BuildAsync();

            Contact contact = contacts2.GetAll().Single();

            //ASSERT
            contact.Name.Should().Be("Alessandro");
        }

        [Fact(DisplayName = "Can recover from snapshot")]
        public async Task Test4()
        {
            //ARRANGE
            string dataPath = NewDataPath;

            RedoDBEngineBuilder<ContactsSystem, IContactsSystem> builder = new();
            builder.WithJsonAdapters(dataPath);
            IContactsSystem contacts = await builder.BuildAsync();
            using (var engine = RedoDBEngine<ContactsSystem>.GetEngine(contacts))
            {
                contacts.AddContact(new() { Name = "Alessandro" });
                RedoDBEngine<ContactsSystem>.GetEngine(contacts).TakeSnapshot();
            }
            //to prove that we are recovering from snapshot, we delete the commandlog, which 
            //should have been closed by the TakeSnapshot method.
            File.Delete(Path.Combine(dataPath, "00000000000000000001.commandlog"));

            //ACT creation of a second instance based on the snapshot of the first one.
            //    system must recover from commandlog.
            RedoDBEngineBuilder<ContactsSystem, IContactsSystem> builder2 = new();
            builder2.WithJsonAdapters(dataPath);
            IContactsSystem contacts2 = await builder2.BuildAsync();

            Contact contact = contacts2.GetAll().Single();

            //ASSERT
            contact.Name.Should().Be("Alessandro");
        }

        [Fact(DisplayName = "Starting and stopping (disposing) the engine 3 times causes 3 logs to be created. " +
            "The snapshot taken at the end must have the same ordinal as the commandlog.")]
        public void Test5()
        {
            //ARRANGE
            string dataPath = NewDataPath;
            //first instance
            RedoDBEngineBuilder<ContactsSystem, IContactsSystem> builder = new();
            builder.WithJsonAdapters(dataPath);
            IContactsSystem contacts = builder.Build();
            using (var engine = RedoDBEngine<ContactsSystem>.GetEngine(contacts))
            {
                contacts.AddContact(new() { Name = "Name1" });
            }
            //second instance
            RedoDBEngineBuilder<ContactsSystem, IContactsSystem> builder2 = new();
            builder2.WithJsonAdapters(dataPath);
            IContactsSystem contacts2 = builder2.Build();
            using (var engine = RedoDBEngine<ContactsSystem>.GetEngine(contacts2))
            {
                contacts2.AddContact(new() { Name = "Name2" });
            }
            //third instance
            RedoDBEngineBuilder<ContactsSystem, IContactsSystem> builder3 = new();
            builder3.WithJsonAdapters(dataPath);
            IContactsSystem contacts3 = builder3.Build();
            using (var engine = RedoDBEngine<ContactsSystem>.GetEngine(contacts3))
            {
                contacts3.AddContact(new() { Name = "Name3" });
                RedoDBEngine<ContactsSystem>.GetEngine(contacts3).TakeSnapshot();
            }

            var files = Directory.GetFiles(dataPath)
                .Select(x => new FileInfo(x).Name)
                .OrderBy(x => x)
                .ToList();
            
            files.Should().HaveCount(4);
           
            files[0].Should().Be("00000000000000000001.commandlog");
            files[1].Should().Be("00000000000000000002.commandlog");
            files[2].Should().Be("00000000000000000003.commandlog");
            files[3].Should().Be("00000000000000000003.snapshot");
        }

        [Fact(DisplayName = "Starting and stopping (disposing) the engine 2 times causes 2 logs to be created. " +
            "The snapshot taken at the end must have the same ordinal as the commandlog.")]
        public void Test6()
        {
            //ARRANGE
            string dataPath = NewDataPath;
            //first instance
            RedoDBEngineBuilder<ContactsSystem, IContactsSystem> builder = new();
            builder.WithJsonAdapters(dataPath);
            IContactsSystem contacts = builder.Build();
            using (var engine = RedoDBEngine<ContactsSystem>.GetEngine(contacts))
            {
                contacts.AddContact(new() { Name = "Name1" });
            }
            //second instance
            RedoDBEngineBuilder<ContactsSystem, IContactsSystem> builder2 = new();
            builder.WithJsonAdapters(dataPath);
            IContactsSystem contacts2 = builder.Build();
            using (var engine = RedoDBEngine<ContactsSystem>.GetEngine(contacts2))
            {
                contacts2.AddContact(new() { Name = "Name2" });
                RedoDBEngine<ContactsSystem>.GetEngine(contacts2).TakeSnapshot();
            }
            
            var files = Directory.GetFiles(dataPath)
                .Select(x => new FileInfo(x).Name)
                .OrderBy(x => x)
                .ToList();

            files.Should().HaveCount(3);
            
            files[0].Should().Be("00000000000000000001.commandlog");
            files[1].Should().Be("00000000000000000002.commandlog");
            files[2].Should().Be("00000000000000000002.snapshot");
        }

        [Fact(DisplayName = "Recovering from log containing property interceptions.")]
        public void Test7()
        {
            //ARRANGE
            string dataPath = NewDataPath;
            RedoDBEngineBuilder<SimpleSystem, ISimpleSystem> builder = new();
            builder.WithJsonAdapters(dataPath);
            ISimpleSystem simple = builder.Build();

            //ACT, ASSERT before recovering
            using (var engine = RedoDBEngine<SimpleSystem>.GetEngine(simple))
            {
                //The default behavior of RedoDB is not to intercept property getters,
                //so no command should be found in the log.
                simple.Value.Should().Be(0);
                engine.Commands.Log.Count().Should().Be(0);

                //For setters the default behavior is interceptions and we expect a 
                //log entry.
                simple.Value = 100;
                engine.Commands.Log.Count().Should().Be(1);
            }

            //ASSERT after recovering
            RedoDBEngineBuilder<SimpleSystem, ISimpleSystem> builder2 = new();
            builder2.WithJsonAdapters(dataPath);
            ISimpleSystem recovered = builder2.Build();

            //ACT
            using (var engine = RedoDBEngine<SimpleSystem>.GetEngine(recovered))
            {
                recovered.Value.Should().Be(100);
            }
        }


        [Fact(DisplayName = "Recovering from snapshots containing property interceptions.")]
        public void Test8()
        {
            //ARRANGE
            string dataPath = NewDataPath;
            RedoDBEngineBuilder<SimpleSystem, ISimpleSystem> builder = new();
            builder.WithJsonAdapters(dataPath);
            ISimpleSystem simple = builder.Build();

            //ACT, ASSERT before recovering
            using (var engine = RedoDBEngine<SimpleSystem>.GetEngine(simple))
            {
                //The default behavior of RedoDB is not to intercept property getters,
                //so no command should be found in the log.
                simple.Value.Should().Be(0);
                engine.Commands.Log.Count().Should().Be(0);

                //For setters the default behavior is interceptions and we expect a 
                //log entry.
                simple.Value = 100;
                engine.Commands.Log.Count().Should().Be(1);
                engine.TakeSnapshot();
            }

            //ASSERT after recovering
            RedoDBEngineBuilder<SimpleSystem, ISimpleSystem> builder2 = new();
            builder2.WithJsonAdapters(dataPath);
            ISimpleSystem recovered = builder2.Build();

            //ACT
            using (var engine = RedoDBEngine<SimpleSystem>.GetEngine(recovered))
            {
                recovered.Value.Should().Be(100);
            }
        }
    }
}
