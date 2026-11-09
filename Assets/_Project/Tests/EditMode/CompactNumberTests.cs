using System.Globalization;
using BackpackAutoBattle.Prototype;
using NUnit.Framework;

namespace BackpackAutoBattle.Tests.Prototype
{
    public sealed class CompactNumberTests
    {
        [TestCase(0L, "0")]
        [TestCase(999L, "999")]
        [TestCase(1000L, "1K")]
        [TestCase(1250L, "1.3K")]
        [TestCase(3200L, "3.2K")]
        [TestCase(999949L, "999.9K")]
        [TestCase(999950L, "1M")]
        [TestCase(1000000L, "1M")]
        [TestCase(1234567L, "1.2M")]
        [TestCase(999950000L, "1B")]
        [TestCase(1000000000L, "1B")]
        [TestCase(9876543210L, "9.9B")]
        [TestCase(-1250L, "-1.3K")]
        [TestCase(long.MaxValue, "9223372036.9B")]
        [TestCase(long.MinValue, "-9223372036.9B")]
        public void FormatsThresholdsAndLargeBalancesWithoutOverflow(long amount, string expected)
            => Assert.That(CompactNumber.Format(amount), Is.EqualTo(expected));

        [Test]
        public void DisplayIsCultureIndependent_AndExactBalanceKeepsEveryDigit()
        {
            var previous = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
                Assert.That(CompactNumber.Format(1234567), Is.EqualTo("1.2M"));
                Assert.That(CompactNumber.Exact(1234567), Is.EqualTo("1,234,567"));
                Assert.That(CompactNumber.Exact(long.MaxValue), Is.EqualTo("9,223,372,036,854,775,807"));
            }
            finally { CultureInfo.CurrentCulture = previous; }
        }
    }
}
