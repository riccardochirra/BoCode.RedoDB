using BoCode.RedoDB.Builder;
using BoCode.RedoDB.DependencyInjection;
using BoCode.RedoDB.Tester.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace BoCode.RedoDB.Tester
{
    public interface IDependency
    {
        void Do();
    }

    public class TestDependency : IDependency
    {
        public void Do() { Console.WriteLine("Done."); }
    }

    public class DIFixture 
    {
        public IServiceProvider Services(string dataPath)
        {
            IServiceCollection serviceCollection = new ServiceCollection();

            serviceCollection.AddRedoDB<IDependency, TestDependency>(builder =>
                builder.WithJsonAdapters(dataPath)
                    .WithCommandlogOnly()
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
            IDependency sut = _serviceProvider.GetService<IDependency>();
            sut.Do();
            RedoDBEngine<TestDependency>.GetEngine<IDependency>(sut).Commands.Log.Count().Should().Be(1);
        }
    }
}
