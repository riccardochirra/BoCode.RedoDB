using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using Xunit.Abstractions;
using BoCode.RedoDB.Tester.Infrastructure;
using BoCode.RedoDB.RedoableSystem;
using BoCode.RedoDB.Builder;

namespace BoCode.RedoDB.Tester
{
    public class CommandlogOnlyTester : TesterWithDataPath
    { 
        public CommandlogOnlyTester(ITestOutputHelper output) : base(output) { }

        [Fact(DisplayName="GIVEN I build RedoDB with the WithCommandlogOnly feature WHEN the system is re-constructed THEN There is no recovering log, indicating that no recovering was taking place.")]
        public void Test1()
        {
            //ARRANGE
            string dataPath = NewDataPath;
            RedoDBEngineBuilder<ContactsSystem, IContactsSystem> builder = new();
            builder.WithJsonAdapters(dataPath);
            builder.WithCommandlogOnly();
            IContactsSystem s = builder.Build();
            s.AddContact(new Contact());
            _ = s.GetAll();
            _ = s.Count();

            //ACT: the engine has not recovered
            RedoDBEngineBuilder<ContactsSystem, IContactsSystem> builder2 = new();
            builder2.WithJsonAdapters(dataPath);
            builder2.WithCommandlogOnly();
            IContactsSystem s2 = builder2.Build();

            //ASSERT
            IRedoDBEngine<ContactsSystem> engine = RedoDBEngine<ContactsSystem>.GetEngine<IContactsSystem>(s2);
            engine.Commands.RecoveringLog.Count().Should().Be(0);
            dataPath.HasFile("00000000000000000001.commandlog").Should().BeTrue();
        }

        [Fact(DisplayName ="GIVEN I'm building a system having WithNoPersistence option WHEN I call WithCommandlogOnly option too THEN an Exception is thrown.")]
        public void Test2()
        {
            //ARRANGE
            string dataPath = NewDataPath;
            RedoDBEngineBuilder<ContactsSystem, IContactsSystem> builder = new();
            builder.WithNoPersistence();
            //ACT,ASSERT
            Action a = ()=>builder.WithCommandlogOnly();
            a.Should().Throw<RedoDBEngineException>().WithMessage("The WithCommandlogOnly option can't be used together with WithNoPersistence!");
        }
    }
}
