using BoCode.RedoDB.Tester.Infrastructure;
using FluentAssertions;
using System;
using System.Linq;
using BoCode.RedoDB.Builder;
using BoCode.RedoDB.RedoableSystem;
using Xunit;
using Xunit.Abstractions;

namespace BoCode.RedoDB.Tester
{
    public class CompensationTester : TesterWithDataPath
    {
        public CompensationTester(ITestOutputHelper output) : base(output) { }

        [Fact(DisplayName = "The error thrown in the command execution code can be caught.")]
        public void Test1()
        {
            //ARRANGE
            string dataPath = NewDataPath;
            //first instance
            RedoDBEngineBuilder<ErrorSystem, IErrorSystem> builder = new();
            builder.WithJsonAdapters(dataPath);
            IErrorSystem system = builder.Build();

            //ACT, ASSERT
            using (var engine = RedoDBEngine<ErrorSystem>.GetEngine(system))
            {
                Action action = () => system.IncreaseValueTo(10, throwExceptionAt: 5);
                system.Value.Should().Be(0);
                action.Should().Throw<ApplicationException>().WithMessage("Exception thrown at 5.");
            }
        }

        /// <summary>
        /// The state of the system after recovering in this test is not relevatn. We expect a usable system even if during recovering there
        /// where exceptions thrown.
        /// </summary>
        [Fact(DisplayName = "What happens while recovering a command causing exceptions? We expect the system to be available.")]
        public void Test2()
        {
            //ARRANGE
            string dataPath = NewDataPath;
            //first instance
            RedoDBEngineBuilder<ErrorSystem, IErrorSystem> builder = new();
            builder.WithJsonAdapters(dataPath);
            IErrorSystem system = builder.Build();

            //ACT, ASSERT
            using (var engine = RedoDBEngine<ErrorSystem>.GetEngine(system))
            {
                Action action = () => system.IncreaseValueTo(10, throwExceptionAt: 5);
                system.Value.Should().Be(0);
                action.Should().Throw<ApplicationException>().WithMessage("Exception thrown at 5.");
                engine.Commands.Log.Count().Should().Be(1);
            }

            RedoDBEngineBuilder<ErrorSystem, IErrorSystem> builder2 = new();
            builder2.WithJsonAdapters(dataPath);
            IErrorSystem recovered = builder2.Build();

            using (var engine = RedoDBEngine<ErrorSystem>.GetEngine(recovered))
            {
                int value = recovered.Value;
                value.Should().Be(5); //It should be five, because there is no compensation activated.
            }
        }

        /// <summary>
        /// This test method verifies that there is a compensation in case of command failure due to internal exceptions raised during command execution. 
        /// The compensation means that the initial state of the system must be restored, so that intermediate invalid states can be avoided.
        /// In the system below valid states are Value = 0 or Value = 10, but the exception will be thrown when the value is internally incremented to 5. 
        /// We want that the value is restored back to 0. A bug in the system if corrected would allow to fix the data too by simply redoing all commands. This is
        /// The reason why we want to save the faulty command into the command log. While recovering a command causing exceptions would compensate too, repeating exactly
        /// what would appen in normal execution of the system.
        /// </summary>
        [Fact(DisplayName = "If an error occours while executing an intercepted command on it, then the state should be the one before command execution. The command should be saved even in case of exceptions.")]
        public void Test3()
        {
            //ARRANGE
            string dataPath = NewDataPath;
            //first instance
            RedoDBEngineBuilder<ErrorSystem, IErrorSystem> builder = new();
            builder.WithJsonAdapters(dataPath);
            builder.WithCompensation();
            IErrorSystem system = builder.Build();

            //ACT, ASSERT
            using (var engine = RedoDBEngine<ErrorSystem>.GetEngine(system))
            {
                Action action = () => system.IncreaseValueTo(10, throwExceptionAt: 5);
                system.Value.Should().Be(0);
                action.Should().Throw<ApplicationException>().WithMessage("Exception thrown at 5.");
                system.Value.Should().Be(0);
                engine.Commands.Log.Count().Should().Be(1);
            }
        }

        [Fact(DisplayName = "Recovering from log does compensate.")]
        public void Test4()
        {
            //ARRANGE
            string dataPath = NewDataPath;
            //first instance
            RedoDBEngineBuilder<ErrorSystem, IErrorSystem> builder = new();
            builder.WithJsonAdapters(dataPath);
            builder.WithCompensation();
            IErrorSystem system = builder.Build();

            //ACT, ASSERT
            using (var engine = RedoDBEngine<ErrorSystem>.GetEngine(system))
            {
                Action action = () => system.IncreaseValueTo(10, throwExceptionAt: 5);
                system.Value.Should().Be(0);
                action.Should().Throw<ApplicationException>().WithMessage("Exception thrown at 5.");
                system.Value.Should().Be(0);
                engine.Commands.Log.Count().Should().Be(1);
            }

            RedoDBEngineBuilder<ErrorSystem, IErrorSystem> builder2 = new();
            builder.WithJsonAdapters(dataPath);
            builder.WithCompensation();
            IErrorSystem recovered = builder.Build();

            //ACT, ASSERT
            using (var engine = RedoDBEngine<ErrorSystem>.GetEngine(recovered))
            {
                system.Value.Should().Be(0);
                engine.Commands.Log.Count().Should().Be(0);
                engine.Commands.RecoveringLog.Count().Should().Be(1);
                engine.Commands.RecoveringLogFaultyCommands.Count().Should().Be(1);
            }
        }


        [Fact(DisplayName = "Compensating more than once works.")]
        public void Test5()
        {
            //ARRANGE
            string dataPath = NewDataPath;
            //first instance
            RedoDBEngineBuilder<ErrorSystem, IErrorSystem> builder = new();
            builder.WithJsonAdapters(dataPath);
            builder.WithCompensation();
            IErrorSystem system = builder.Build();

            //ACT, ASSERT
            using (var engine = RedoDBEngine<ErrorSystem>.GetEngine(system))
            {
                Action action = () => system.IncreaseValueTo(10, throwExceptionAt: 5);
                system.Value.Should().Be(0);
                action.Should().Throw<ApplicationException>().WithMessage("Exception thrown at 5."); //Command1
                system.Value.Should().Be(0);

                //set value to 10
                system.Value = 10; //Command2

                //rise to 20 but throw exception at 15
                Action action2 = () => system.IncreaseValueTo(20, throwExceptionAt: 15);
                system.Value.Should().Be(10);
                action2.Should().Throw<ApplicationException>().WithMessage("Exception thrown at 15."); //Command3
                system.Value.Should().Be(10);

                engine.Commands.Log.Count().Should().Be(3);
            }
        }

        [Fact(DisplayName = "Compensating after takesnapshot works.")]
        public void Test6()
        {
            //ARRANGE
            string dataPath = NewDataPath;
            //first instance
            RedoDBEngineBuilder<ErrorSystem, IErrorSystem> builder = new();
            builder.WithJsonAdapters(dataPath);
            builder.WithCompensation();
            IErrorSystem system = builder.Build();

            //ACT, ASSERT
            using (var engine = RedoDBEngine<ErrorSystem>.GetEngine(system))
            {
                Action action = () => system.IncreaseValueTo(10, throwExceptionAt: 5);
                system.Value.Should().Be(0);
                action.Should().Throw<ApplicationException>().WithMessage("Exception thrown at 5."); //Command1
                system.Value.Should().Be(0);

                //set value to 10
                system.Value = 10; //Command2
                //take snapthot
                engine.TakeSnapshot();

                //rise to 20 but throw exception at 15
                Action action2 = () => system.IncreaseValueTo(20, throwExceptionAt: 15);
                system.Value.Should().Be(10);
                action2.Should().Throw<ApplicationException>().WithMessage("Exception thrown at 15."); //Command3
                system.Value.Should().Be(10);

                engine.Commands.Log.Count().Should().Be(1); //the new log should contain only command3
            }
        }


    }
}
