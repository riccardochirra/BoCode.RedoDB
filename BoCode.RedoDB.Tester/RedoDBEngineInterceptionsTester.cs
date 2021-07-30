using BoCode.RedoDB.RedoableSystem;
using FluentAssertions;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace BoCode.RedoDB.Tester
{
    public class RedoDBEngineInterceptionsTester
    {
        [Fact(DisplayName = "Intercept method")]
        public async Task Test1()
        {
            RedoDBEngineBuilder<ContactsSystem, IContactsSystem> builder = new();
            builder.WithNoPersistence();
            IContactsSystem contacts = await builder.BuildAsync();

            contacts.AddContact(new Contact());

            IRedoDBEngine<ContactsSystem> engine = contacts as IRedoDBEngine<ContactsSystem>;

            engine.Commands.Log.Last().MemberName.Should().Be("AddContact");
        }

        [Fact(DisplayName = "Intercept only restricted method")]
        public async Task Test2()
        {
            //ARRANGE
            RedoDBEngineBuilder<ContactsSystem, IContactsSystem> builder = new();
            //as soon you tell what to intercept, only instructed interceptions will be executed
            builder.WithNoPersistence();
            builder.Intercept(nameof(ContactsSystem.AddContact));
            IContactsSystem contacts = await builder.WithNoPersistence().BuildAsync();

            //ACT
            //this method will be intercepted
            contacts.AddContact(new Contact());
            //this method will not be intercepted
            contacts.GetAll();

            //ASSERT
            IRedoDBEngine<ContactsSystem> engine = contacts as IRedoDBEngine<ContactsSystem>;
            var instructions = engine.Instructions;
            instructions.CanIntercept("AddContact").Should().BeTrue();

            engine.Commands.Log.Last().MemberName.Should().Be("AddContact");
        }

        [Fact(DisplayName = "Intercept all methods")]
        public async Task Test3()
        {
            RedoDBEngineBuilder<ContactsSystem, IContactsSystem> builder = new();
            builder.WithNoPersistence();
            IContactsSystem contacts = await builder.WithNoPersistence().BuildAsync();

            contacts.AddContact(new Contact());
            int count = contacts.Count();

            IRedoDBEngine<ContactsSystem> engine = contacts as IRedoDBEngine<ContactsSystem>;
            engine.Commands.Log.Count().Should().Be(2);
        }

        [Fact(DisplayName = "Intercept only methods not beginning with 'Get'")]
        public async Task Test4()
        {
            //ARRANGE
            RedoDBEngineBuilder<ContactsSystem, IContactsSystem> builder = new();
            builder.WithNoPersistence();
            //as soon you tell waht to intercept, only instructed interceptions will be executed
            builder.ExcludeMethodsStartingWith("Get");
            IContactsSystem contacts = await builder.WithNoPersistence().BuildAsync();

            //ACT
            //this method will be intercepted
            contacts.AddContact(new Contact());
            //this method will not be intercepted
            _ = contacts.GetAll();

            //ASSERT
            IRedoDBEngine<ContactsSystem> engine = contacts as IRedoDBEngine<ContactsSystem>;
            var instructions = engine.Instructions;
            instructions.CanIntercept("GetAll").Should().BeFalse();
            engine.Commands.Log.Single().MemberName.Should().Be("AddContact");
        }

        [Fact(DisplayName = "Get interceptions automatically by the interface using RedoableAttribute")]
        public async Task Test5()
        {
            //ARRANGE
            RedoDBEngineBuilder<AccountingSystem, IAccountingSystem> builder = new();
            builder.WithNoPersistence();
            //no need to specify how to intercept, the IAccountinSystem interface declared the method AddAccount as redoable.
            IAccountingSystem accounts = await builder.WithNoPersistence().BuildAsync();

            //ASSERT
            IRedoDBEngine<AccountingSystem> engine = accounts as IRedoDBEngine<AccountingSystem>;
            var instructions = engine.Instructions;
            instructions.CanIntercept("AddAccount").Should().BeTrue();
            instructions.CanIntercept("GetAll").Should().BeFalse();
        }
    }
}
