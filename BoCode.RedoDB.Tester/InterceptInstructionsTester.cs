using BoCode.RedoDB.Interception;
using FluentAssertions;
using Xunit;

namespace BoCode.RedoDB.Tester
{
    public class InterceptInstructionsTester
    {
        [Fact(DisplayName = "Can intercept method")]
        public void Test1()
        {
            //ARRANGE
            InterceptionsManager instructions = new();
            //ACT
            instructions.AddInterception("AMethodName");
            //ASSERT
            instructions.CanIntercept("AMethodName").Should().BeTrue();
        }

        [Fact(DisplayName = "If no instruction is added, al methods are intercepted")]
        public void Test2()
        {
            //ARRANGE
            InterceptionsManager instructions = new();
            //ASSERT
            instructions.CanIntercept("AMethodName").Should().BeTrue();
        }

        [Fact(DisplayName = "Instructions are present, the method does not belong to the list and can't be intercepted.")]
        public void Test3()
        {
            //ARRANGE
            InterceptionsManager instructions = new();
            //ACT
            instructions.AddInterception("otherMethod");
            //ASSERT
            instructions.CanIntercept("my-method-is-not-added").Should().BeFalse();
        }

        [Fact(DisplayName = "Exclusion of methods starting with 'Get'")]
        public void Test4()
        {
            //ARRANGE
            InterceptionsManager interceptions = new();
            //ACT
            interceptions.ExcludeMembersStartingWith("Get");
            //ASSERT
            interceptions.CanIntercept("GetData").Should().BeFalse();
            interceptions.CanIntercept("SetData").Should().BeTrue();
        }

        [Fact(DisplayName = "More than one exclusion is supported")]
        public void Test5()
        {
            //ARRANGE
            InterceptionsManager interceptions = new();
            //ACT
            interceptions.ExcludeMembersStartingWith("Get");
            interceptions.ExcludeMembersStartingWith("TryGet");
            //ASSERT
            interceptions.CanIntercept("GetData").Should().BeFalse();
            interceptions.CanIntercept("TryGetData").Should().BeFalse();
            interceptions.CanIntercept("SetData").Should().BeTrue();
        }


    }
}
