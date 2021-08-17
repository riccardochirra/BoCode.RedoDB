using BoCode.RedoDB.RedoableSystem;
using BoCode.RedoDB.Tester.Infrastructure;
using FluentAssertions;
using System;
using System.Linq;
using BoCode.RedoDB.Builder;
using Xunit;
using Xunit.Abstractions;

namespace BoCode.RedoDB.Tester
{
    public class RedoDBRedoableTester : TesterWithDataPath
    {
        public RedoDBRedoableTester(ITestOutputHelper output) : base(output) { }

        [Fact(DisplayName = "GIVEN a System implementing IDependOnRedoableGuid and IDependOnRedoableClock WHEN I use a method generating a Guid and recover the system THEN The same Guid is returned.")]
        public void Test()
        {
            //ARRANGE
            Guid? guid1 = null;
            Guid? guid2 = null;
            DateTime? timeStamp1;
            DateTime? timeStamp2;
            string dataPath = NewDataPath;
            //first instance
            RedoDBEngineBuilder<ContactsSystem, IContactsSystem> builder = new();
            builder.WithJsonAdapters(dataPath);
            IContactsSystem contacts = builder.Build();
            using (var engine = RedoDBEngine<ContactsSystem>.GetEngine(contacts))
            {
                Contact c = contacts.CreateContact();
                guid1 = c.Id;
                timeStamp1 = c.TimeStamp;
                engine.Commands.Log.Single().MemberName.Should().Be("CreateContact"); //Internal AddContact called from CreateContact is not interceped.
            }

            //ACT, recover from commandlog
            RedoDBEngineBuilder<ContactsSystem, IContactsSystem> builder2 = new();
            builder2.WithJsonAdapters(dataPath);
            builder2.ExcludeMethodsStartingWith("Get");
            builder2.ExcludeMethodsStartingWith("Count");
            IContactsSystem contacts2 = builder2.Build();
            using (var engine = RedoDBEngine<ContactsSystem>.GetEngine(contacts2))
            {
                Contact c = contacts2.GetAll().Single();
                guid2 = c.Id;
                timeStamp2 = c.TimeStamp;
                engine.Commands.Log.Count().Should().Be(0);
            }

            //ASSERT
            guid2.Should().Be(guid1);
            timeStamp2.Should().Be(timeStamp2);
        }

    }
}
