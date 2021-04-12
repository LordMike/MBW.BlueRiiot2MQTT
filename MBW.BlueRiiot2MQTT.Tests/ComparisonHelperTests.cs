using System;
using Xunit;
using MBW.BlueRiiot2MQTT.Helpers;

namespace MBW.BlueRiiot2MQTT.Tests
{
    public class ComparisonHelperTests
    {
        [Fact]
        public void GetMaxTest()
        {
            DateTime a = DateTime.Now;
            DateTime b = a.AddHours(1);
            
            Assert.Equal(b, ComparisonHelper.GetMax<DateTime>(a, b));
            Assert.Equal(b, ComparisonHelper.GetMax<DateTime>(b, a));
        }

        [Fact]
        public void GetMinTest()
        {
            DateTime a = DateTime.Now;
            DateTime b = a.AddHours(1);
            
            Assert.Equal(a, ComparisonHelper.GetMin<DateTime>(a, b));
            Assert.Equal(a, ComparisonHelper.GetMin<DateTime>(b, a));
        }
    }
}
