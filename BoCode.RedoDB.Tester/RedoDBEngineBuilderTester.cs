using BoCode.RedoDB.Builder;
using BoCode.RedoDB.RedoableSystem;
using FluentAssertions;
using System;
using System.Threading.Tasks;
using Xunit;

namespace BoCode.RedoDB.Tester
{
    public class RedoDBEngineBuilderTester
    {
        [Fact(DisplayName = "GIVEN a builder using default persistence adapters (commands, snapshots) with no data path given " +
                            "WHEN Build is called " +
                            "THEN The MissingConfigurationException is expected.")]
        public void Test1()
        {
            //ARRANGE
            RedoDBEngineBuilder<ContactsSystem, IContactsSystem> builder = new();
            //ACT, ASSERT
            Func<Task> action = async () => await builder.BuildAsync();
            action.Should().ThrowAsync<RedoDBEngineBuilderException>()
                .WithMessage("JsonCommandAdapter needs a data path. Use the builder WithDataPath method do configure it before build.");
        }

        [Fact(DisplayName = "GIVEN a builder using default persistence adapters (commands, snapshots) with no data path given " +
                            "WHEN WithNoPersitence is called " +
                            "THEN Build can be called without getting the MissingBuilderConfiurationException, but TakeSnapshot() would cause a RedoDBEngineException")]
        public void Test2()
        {
            //ARRANGE
            IContactsSystem contacts = null;
            RedoDBEngineBuilder<ContactsSystem, IContactsSystem> builder = new();
            builder.WithNoPersistence();
            //ACT, ASSERT
            Func<Task> build = async () => contacts = await builder.BuildAsync();
            build.Should().NotThrowAsync<RedoDBEngineBuilderException>();

            Action takeSnapshot = () => RedoDBEngine<ContactsSystem>.GetEngine(contacts).TakeSnapshot();
            takeSnapshot.Should().Throw<RedoDBEngineException>()
                .WithMessage("This engine has been configured to intercept method calls only.No persistence is deactivated.TakeSnapshot can't be used.");
        }

        [Fact(DisplayName = "Can't build if system does not implement the interface.")]
        public void Test3()
        {
            //ARRANGE, note the combination of Account and IContactSystem while creating the builder (wrong!)
            IContactsSystem contacts = null;
            RedoDBEngineBuilder<Account, IContactsSystem> builder = new();
            builder.WithNoPersistence();
            //ACT, ASSERT
            Func<Task> build = async () => contacts = await builder.BuildAsync();
            build.Should().ThrowAsync<RedoDBEngineException>().WithMessage("System T does not implement interface I!");
        }

        [Fact(DisplayName = "Can't GetEngine if system is not of the generic type given.")]
        public void Test4()
        {
            //ARRANGE
            RedoDBEngineBuilder<ContactsSystem, IContactsSystem> builder = new();
            builder.WithNoPersistence();
            IContactsSystem contacts = builder.Build();

            //ACT, ASSERT. Note the wrong combination of AccountingSystem generic type and contacts argument.
            Action getEngine = () => RedoDBEngine<AccountingSystem>.GetEngine(contacts);
            getEngine.Should().Throw<RedoDBEngineException>();
        }
    }
}
