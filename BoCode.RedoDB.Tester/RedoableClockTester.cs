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
            DateTime check = DateTime.Now;

            var clock = new RedoableClock();

            DateTime now = clock.Now;

            now.Should().HaveYear(check.Year);
            now.Should().HaveMonth(check.Month);    
            now.Should().HaveDay(check.Day);
        }

        [Fact(DisplayName = "GIVEN RedoableClock is redoing WHEN I ask for a new DateTime using Now THEN I get a new DateTime")]
        public void Test2()
        {
            var clock = new RedoableClock();

            clock.Redoing(new List<DateTime> { new DateTime(2021, 7, 20) });

            DateTime now = clock.Now;

            now.Should().HaveYear(2021);
            now.Should().HaveMonth(7);
            now.Should().HaveDay(20);
            now.Should().HaveMinute(0);
            now.Should().HaveHour(0);
            now.Should().HaveSecond(0);
        }
    }
}
