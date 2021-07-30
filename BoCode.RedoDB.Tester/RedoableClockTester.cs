using BoCode.RedoDB.RedoableData;
using FluentAssertions;
using System;
using System.Collections.Generic;
using Xunit;

namespace BoCode.RedoDB.Tester
{
    public class RedoableClockTester
    {
        [Fact(DisplayName = "GIVEN RedoableClock is not redoing WHEN I ask for a new DateTime using Now THEN I get a new DateTime")]
        public void Test1()
        {
            var clock = RedoableClock.Singleton();

            DateTime now = clock.Now;

            DateTime.Now.Should().HaveMinute(now.Minute);
        }

        [Fact(DisplayName = "GIVEN RedoableClock is not redoing WHEN I ask for a new DateTime using Now THEN I get a new DateTime")]
        public void Test2()
        {
            var clock = RedoableClock.Singleton();

            clock.Redoing(new List<DateTime> { new DateTime(2021, 7, 20) });

            DateTime now = clock.Now;

            now.Should().HaveMinute(0);
            now.Should().HaveHour(0);
            now.Should().HaveSecond(0);
        }
    }
}
