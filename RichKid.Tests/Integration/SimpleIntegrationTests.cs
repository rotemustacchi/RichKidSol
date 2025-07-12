using Xunit;

namespace RichKid.Tests.Integration
{
    /// <summary>
    /// Simple integration tests that don't require data
    /// These tests verify the basic testing infrastructure works
    /// </summary>
    public class SimpleIntegrationTests
    {
        [Fact]
        public void SimpleTest_ShouldPass()
        {
            // Just verify that our test infrastructure is working
            Assert.True(true);
        }

        [Fact]
        public void BasicMath_ShouldWork()
        {
            // Verify basic calculations work in test environment
            var result = 2 + 2;
            Assert.Equal(4, result);
        }

        [Fact]
        public void StringOperations_ShouldWork()
        {
            // Verify string operations work in test environment
            var greeting = "Hello" + " " + "World";
            Assert.Equal("Hello World", greeting);
        }

        [Theory]
        [InlineData(1, 1, 2)]
        [InlineData(2, 3, 5)]
        [InlineData(-1, 1, 0)]
        public void Addition_ShouldReturnCorrectResult(int a, int b, int expected)
        {
            // Test with multiple data sets
            var result = a + b;
            Assert.Equal(expected, result);
        }
    }
}