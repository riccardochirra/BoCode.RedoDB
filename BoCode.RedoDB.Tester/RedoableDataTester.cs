using BoCode.RedoDB.RedoableData;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace BoCode.RedoDB.Tester
{

    public class RedoableDataTester
    {
        [Fact(DisplayName = "A new RedoableData is not redoing.")]
        public void Test2()
        {
            //ASSERT
            new RedoableData<Guid>(() => Guid.NewGuid()).IsRedoing.Should().BeFalse();
        }

        [Fact(DisplayName = "GIVEN a RedoableData is not redoing " +
                            "WHEN I ask for a new value " +
                            "THEN the value is tracked and IsRedoing returns false.")]
        public void Test1()
        {
            //ARRANGE
            RedoableData<Guid> redoableGuid = new RedoableData<Guid>(() => Guid.NewGuid());

            //ACT
            Guid newValue = redoableGuid.New();

            //ASSERT
            redoableGuid.Tracked.Single().Should().Be(newValue);
            redoableGuid.IsRedoing.Should().BeFalse();
        }

        [Fact(DisplayName = "GIVEN a set of values is injected in the RedoableData object " +
                            "WHEN I query IsRedoing " +
                            "THEN the returned value is true.")]
        public void Test3()
        {
            //ARRANGE
            RedoableData<Guid> redoableGuid = new RedoableData<Guid>(() => Guid.NewGuid());
            redoableGuid.Redoing(new List<Guid> { Guid.NewGuid() });

            //ASSERT
            redoableGuid.IsRedoing.Should().BeTrue();
        }



        [Fact(DisplayName = "GIVEN a value is injected in the RedoableData object " +
                            "WHEN I ask for a new value " +
                            "THEN the returned value is the tracked one.")]
        public void Test4()
        {
            //ARRANGE
            RedoableData<Guid> redoableGuid = new RedoableData<Guid>(() => Guid.NewGuid());
            redoableGuid.Redoing(new List<Guid> { new Guid("{a1b77983-52f0-4d58-b83b-3fde9b560194}") });

            //ACT
            Guid newValue = redoableGuid.New();

            //ASSERT
            newValue.Should().Be(new Guid("{a1b77983-52f0-4d58-b83b-3fde9b560194}"));
        }

        [Fact(DisplayName = "GIVEN a set of two values is injected in the RedoableData object " +
                    "WHEN I ask for a new value " +
                    "THEN the returned value is the first tracked one AND the list of tracked counts now only 1 remaining value.")]
        public void Test5()
        {
            //ARRANGE
            RedoableData<Guid> redoableGuid = new RedoableData<Guid>(() => Guid.NewGuid());
            redoableGuid.Redoing(new List<Guid> {
                new Guid("{a1b77983-52f0-4d58-aaaa-3fde9b560194}" ),
                new Guid("{a1b77983-52f0-4d58-bbbb-3fde9b560194}") });

            //ACT
            Guid newValue = redoableGuid.New();

            //ASSERT
            newValue.Should().Be(new Guid("{a1b77983-52f0-4d58-aaaa-3fde9b560194}"));
            redoableGuid.IsRedoing.Should().BeTrue();
            redoableGuid.Tracked.Single().Should().Be(new Guid("{a1b77983-52f0-4d58-bbbb-3fde9b560194}"));
        }

        [Fact(DisplayName = "GIVEN a set of two values is injected in the RedoableData object " +
            "WHEN I ask for a new value twice " +
            "THEN the last returned value is the last tracked one AND the list of tracked is empty AND IsRedoing is false.")]
        public void Test6()
        {
            //ARRANGE
            RedoableData<Guid> redoableGuid = new RedoableData<Guid>(() => Guid.NewGuid());
            redoableGuid.Redoing(new List<Guid> {
                new Guid("{a1b77983-52f0-4d58-aaaa-3fde9b560194}" ),
                new Guid("{a1b77983-52f0-4d58-bbbb-3fde9b560194}") });

            //ACT
            _ = redoableGuid.New();
            Guid newValue = redoableGuid.New();

            //ASSERT
            newValue.Should().Be(new Guid("{a1b77983-52f0-4d58-bbbb-3fde9b560194}"));
            redoableGuid.IsRedoing.Should().BeFalse();
            redoableGuid.Tracked.Count().Should().Be(0);
        }

        [Fact(DisplayName = "GIVEN a set of two values is injected in the RedoableData object " +
    "WHEN I try to inject another set " +
    "THEN a RedoDBException is thrown")]
        public void Test7()
        {
            //ARRANGE
            RedoableData<Guid> redoableGuid = new RedoableData<Guid>(() => Guid.NewGuid());
            redoableGuid.Redoing(new List<Guid> {
                new Guid("{a1b77983-52f0-4d58-aaaa-3fde9b560194}" ),
                new Guid("{a1b77983-52f0-4d58-bbbb-3fde9b560194}") });

            //ACT, ASSERT
            Action redoingAgain = () => redoableGuid.Redoing(new List<Guid> {
                new Guid("{a1b77983-52f0-4d58-cccc-3fde9b560194}" ),
                new Guid("{a1b77983-52f0-4d58-dddd-3fde9b560194}") });

            redoingAgain.Should().Throw<RedoDBRedoableException>();
        }
    }
}
