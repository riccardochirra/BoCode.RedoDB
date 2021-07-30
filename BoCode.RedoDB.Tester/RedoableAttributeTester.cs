using BoCode.RedoDB.RedoableSystem;
using FluentAssertions;
using System.Linq;
using Xunit;

namespace BoCode.RedoDB.Tester
{
    public class RedoableAttributeTester
    {
        [Fact(DisplayName = "Get redoable method from interface")]
        public void Test1()
        {
            var methodNames = RedoableInspector<IAccountingSystem>.GetAllRedoableMethodNames();
            methodNames.Count().Should().Be(1);
        }

        [Fact(DisplayName = "Get redoable method from class returns empty list")]
        public void Test2()
        {
            var methodNames = RedoableInspector<AccountingSystem>.GetAllRedoableMethodNames();
            methodNames.Count().Should().Be(0);
        }

        [Fact(DisplayName = "Get redoable method from test class " +
            "having a method with RedoableAttribute returns the method name.")]
        public void Test3()
        {
            var methodNames = RedoableInspector<TestClass>.GetAllRedoableMethodNames();
            methodNames.Count().Should().Be(2);
        }
    }
}
