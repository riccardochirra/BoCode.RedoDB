using BoCode.RedoDB.Builder;
using BoCode.RedoDB.DependencyInjection;
using BoCode.RedoDB.Persistence.Commands;
using BoCode.RedoDB.Tester.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace BoCode.RedoDB.Tester
{
    public interface ISystem
    {
        void Do();
    }

    public interface IDependencyOfSystem
    {
        public void Write();
    }

    public class Writer : IDependencyOfSystem
    {
        public void Write()
        {
            Console.WriteLine("Done.");
        }
    }

    public class SystemWithDependency : ISystem
    {
        private readonly IDependencyOfSystem _writer = null;
        public SystemWithDependency(IDependencyOfSystem writer)
        {
            _writer = writer;
        }
        public void Do() { _writer.Write(); }
    }

    public class DIFixture 
    {
        public IServiceProvider Services(string dataPath)
        {
            IServiceCollection serviceCollection = new ServiceCollection();

            serviceCollection.AddSingleton<IDependencyOfSystem>(new Writer());

            serviceCollection.AddRedoDB<ISystem, SystemWithDependency>(builder =>
                builder.WithCommandlogOnly(new JsonCommandAdapter(new DirectoryInfo(dataPath), new CommandlogNameProvider()))
                    .Build()
                ); 

            return serviceCollection.BuildServiceProvider();
        }
    }
    /// <summary>
    /// This test class contains test proving that RedoDB can be used together with DI-Frameworks
    /// </summary>
    public class DependencyInjectionTester: TesterWithDataPath, IClassFixture<DIFixture>
    {
        private IServiceProvider _serviceProvider;

        public DependencyInjectionTester(ITestOutputHelper output, DIFixture fixture) : base(output) 
        {
            _serviceProvider = fixture.Services(NewDataPath);
        }

        [Fact(DisplayName="The redoable system has dependencies and can be constructed using DI.")]
        public void Test1()
        {
            ISystem sut = _serviceProvider.GetService<ISystem>();
            sut.Do();
            RedoDBEngine<SystemWithDependency>.GetEngine<ISystem>(sut).Commands.Log.Count().Should().Be(1);
        }
    }
}
