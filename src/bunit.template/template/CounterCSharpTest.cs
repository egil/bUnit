#if (testFramework_tunit)
using TUnit.Core;
#endif

namespace Company.BlazorTests1;

/// <summary>
/// These tests are written entirely in C#.
/// Learn more at https://bunit.dev/docs/getting-started/writing-tests.html#creating-basic-tests-in-cs-files
/// </summary>
#if (testFramework_xunit)
public class CounterCSharpTest : BunitContext
#elif (testFramework_xunitv3)
// The full qualified namespace for bUnit TestContext is used here as xunit v3 also offers a TestContext class
public class CounterCSharpTest : BunitContext
#elif (testFramework_nunit)
public class CounterCSharpTest : BunitContext
#elif (testFramework_mstest)
[TestClass]
public class CounterCSharpTest : BunitContext
#elif (testFramework_tunit)
[Test]
public class CounterCSharpTest : BunitContext
#endif
{
#if (testFramework_xunit)
	[Fact]
#elif (testFramework_xunitv3)
	[Fact]
#elif (testFramework_nunit)
	[Test]
#elif (testFramework_mstest)
	[TestMethod]
#endif
	public void CounterStartsAtZero()
{
    // Arrange
    var cut = Render<Counter>();

    // Assert that content of the paragraph shows counter at zero
    cut.Find("p").MarkupMatches("<p>Current count: 0</p>");
}

#if (testFramework_xunit)
	[Fact]
#elif (testFramework_xunitv3)
	[Fact]
#elif (testFramework_nunit)
	[Test]
#elif (testFramework_mstest)
	[TestMethod]
#endif
public void ClickingButtonIncrementsCounter()
{
    // Arrange
    var cut = Render<Counter>();

    // Act - click button to increment counter
    cut.Find("button").Click();

    // Assert that the counter was incremented
    cut.Find("p").MarkupMatches("<p>Current count: 1</p>");
}
}
