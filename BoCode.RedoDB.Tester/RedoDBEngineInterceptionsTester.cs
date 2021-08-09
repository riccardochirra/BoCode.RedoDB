using BoCode.RedoDB.RedoableSystem;
using FluentAssertions;
using System.Linq;
using System.Threading.Tasks;
using BoCode.RedoDB.Builder;
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
            instructions.CanIntercept("Count").Should().BeFalse();

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

        [Fact(DisplayName ="Getter are ignored by default (not intercepted).")]
        public async Task Test6()
        {
            //ARRANGE
            RedoDBEngineBuilder<ContactsSystem, IContactsSystem> builder = new();
            builder.WithNoPersistence();
            IContactsSystem contacts = await builder.BuildAsync();

            //ASSERT
            IRedoDBEngine<ContactsSystem> engine = RedoDBEngine<ContactsSystem>.GetEngine<IContactsSystem>(contacts);
            var instructions = engine.Instructions;
            instructions.CanInterceptGetter("SomeInfo").Should().BeFalse();
        }

        [Fact(DisplayName = "Setter is intercepted by default.")]
        public async Task Test7()
        {
            //ARRANGE
            RedoDBEngineBuilder<ContactsSystem, IContactsSystem> builder = new();
            builder.WithNoPersistence();
            IContactsSystem contacts = await builder.BuildAsync();

            //ASSERT
            IRedoDBEngine<ContactsSystem> engine = RedoDBEngine<ContactsSystem>.GetEngine<IContactsSystem>(contacts);
            var instructions = engine.Instructions;
            instructions.CanInterceptSetter("SomeInfo").Should().BeTrue();            //instructions.CanIntercept("SomeInfo").Should()
        }

        [Fact(DisplayName = "Setter can be excluded using the ")]
        public async Task Test8()
        {
            //ARRANGE
            RedoDBEngineBuilder<ContactsSystem, IContactsSystem> builder = new();
            builder.WithNoPersistence();
            IContactsSystem contacts = await builder.BuildAsync();

            //ASSERT
            IRedoDBEngine<ContactsSystem> engine = RedoDBEngine<ContactsSystem>.GetEngine<IContactsSystem>(contacts);
            var instructions = engine.Instructions;
            instructions.CanInterceptSetter("SomeInfo").Should().BeTrue(); //you can be more explicitely by using CanIntercepSeter, but this method does the same as CanIntercept.
            instructions.CanIntercept("SomeInfo").Should().BeTrue(); 
        }

        [Fact(DisplayName = "If you restrict interception the setter, if not included in restriction, is excluded")]
        public async Task Test9()
        {
            //ARRANGE
            RedoDBEngineBuilder<ContactsSystem, IContactsSystem> builder = new();
            builder.WithNoPersistence();
            builder.Intercept("AddContact");
            IContactsSystem contacts = await builder.BuildAsync();

            //ASSERT
            IRedoDBEngine<ContactsSystem> engine = RedoDBEngine<ContactsSystem>.GetEngine<IContactsSystem>(contacts);
            var instructions = engine.Instructions;
            instructions.CanInterceptSetter("SomeInfo").Should().BeFalse();            
        }


        [Fact(DisplayName = "Setter can be excluded.")]
        public async Task Test10()
        {
            //ARRANGE
            RedoDBEngineBuilder<ContactsSystem, IContactsSystem> builder = new();
            builder.WithNoPersistence();
            builder.ExcludeMethodsStartingWith("SomeInfo");
            IContactsSystem contacts = await builder.BuildAsync();

            //ASSERT
            IRedoDBEngine<ContactsSystem> engine = RedoDBEngine<ContactsSystem>.GetEngine<IContactsSystem>(contacts);
            var instructions = engine.Instructions;
            instructions.CanInterceptSetter("SomeInfo").Should().BeFalse();
        }

        [Fact(DisplayName = "Getter can be included.")]
        public async Task Test11()
        {
            //ARRANGE
            RedoDBEngineBuilder<ContactsSystem, IContactsSystem> builder = new();
            builder.WithNoPersistence();
            builder.InterceptGetter("SomeInfo");
            IContactsSystem contacts = await builder.BuildAsync();

            //ASSERT
            IRedoDBEngine<ContactsSystem> engine = RedoDBEngine<ContactsSystem>.GetEngine<IContactsSystem>(contacts);
            var instructions = engine.Instructions;
            instructions.CanInterceptGetter("SomeInfo").Should().BeTrue();
        }

    }
}
