using Bignum.Core.Bignum;

namespace Bignum.Tests.Core.Bignum;

public class BignumHelperTests
{
    // Helper method to create a NodeChain from array values
    private static NodeChain CreateChain(params int[] values)
    {
        if (values == null || values.Length == 0) return NodeChain.Empty;
        var dummy = new BignumNode(0);
        var current = dummy;
        foreach (var val in values)
        {
            current.Next = new BignumNode(val);
            current = current.Next;
        }

        return new NodeChain(dummy.Next, values.Length);
    }

    // Helper method to assert equality of two NodeChains
    private static void AssertChainEqual(NodeChain expected, NodeChain actual)
    {
        Assert.Equal(expected.NodeCount, actual.NodeCount);
        var currExp = expected.Head;
        var currAct = actual.Head;
        while (currExp != null && currAct != null)
        {
            Assert.Equal(currExp.Value, currAct.Value);
            currExp = currExp.Next;
            currAct = currAct.Next;
        }

        Assert.Null(currExp);
        Assert.Null(currAct);
    }

    #region AddRaw Tests

    [Fact]
    public void AddRaw_EmptyChains_ShouldReturnEmpty()
    {
        var result = BignumHelper.AddRaw(NodeChain.Empty, NodeChain.Empty);
        Assert.True(result.IsEmpty);
    }

    [Fact]
    public void AddRaw_OneEmptyChain_ShouldReturnOther()
    {
        var x = CreateChain(1234);
        var result1 = BignumHelper.AddRaw(x, NodeChain.Empty);
        var result2 = BignumHelper.AddRaw(NodeChain.Empty, x);

        AssertChainEqual(x, result1);
        AssertChainEqual(x, result2);
    }

    [Fact]
    public void AddRaw_NoCarry_ShouldAddCorrectly()
    {
        var x = CreateChain(1234, 5678);
        var y = CreateChain(1111, 2222);
        var expected = CreateChain(2345, 7900);

        var result = BignumHelper.AddRaw(x, y);

        AssertChainEqual(expected, result);
    }

    [Fact]
    public void AddRaw_WithCarry_ShouldCarryOver()
    {
        var x = CreateChain(9000, 9999);
        var y = CreateChain(2000, 1);
        // 9000 + 2000 = 11000 (1000, carry 1)
        // 9999 + 1 + 1 (carry) = 10001 (1, carry 1)
        // final carry = 1
        // Expected nodes: 1000 -> 1 -> 1
        var expected = CreateChain(1000, 1, 1);

        var result = BignumHelper.AddRaw(x, y);

        AssertChainEqual(expected, result);
    }

    #endregion

    #region SubtractRaw Tests

    [Fact]
    public void SubtractRaw_EqualChains_ShouldReturnEmpty()
    {
        var x = CreateChain(1234, 5678);
        var result = BignumHelper.SubtractRaw(x, x);
        Assert.True(result.IsEmpty);
    }

    [Fact]
    public void SubtractRaw_MinuendLessThanSubtrahend_ShouldThrow()
    {
        var x = CreateChain(1234);
        var y = CreateChain(5678);

        Assert.Throws<InvalidOperationException>(() => BignumHelper.SubtractRaw(x, y));
    }

    [Fact]
    public void SubtractRaw_SimpleSubtraction_ShouldSubtract()
    {
        var x = CreateChain(5678, 9999);
        var y = CreateChain(1234, 1111);
        var expected = CreateChain(4444, 8888);

        var result = BignumHelper.SubtractRaw(x, y);

        AssertChainEqual(expected, result);
    }

    [Fact]
    public void SubtractRaw_WithBorrow_ShouldBorrowCorrectly()
    {
        var x = CreateChain(0, 1); // 10000
        var y = CreateChain(1); // 1
        var expected = CreateChain(9999); // 9999

        var result = BignumHelper.SubtractRaw(x, y);

        AssertChainEqual(expected, result);
    }

    [Fact]
    public void SubtractRaw_WithTrailingZeros_ShouldCleanTail()
    {
        // 56781234 - 56781111 = 123 (stored as [123])
        // raw subtraction yields [123, 0], which should be cleaned to [123]
        var x = CreateChain(1234, 5678);
        var y = CreateChain(1111, 5678);
        var expected = CreateChain(123);

        var result = BignumHelper.SubtractRaw(x, y);

        AssertChainEqual(expected, result);
    }

    #endregion

    #region MultiplyDigit Tests

    [Fact]
    public void MultiplyDigit_MultiplyByZero_ShouldReturnEmpty()
    {
        var x = CreateChain(1234, 5678);
        var result = BignumHelper.MultiplyDigit(x, 0);
        Assert.True(result.IsEmpty);
    }

    [Fact]
    public void MultiplyDigit_MultiplyByOne_ShouldReturnSame()
    {
        var x = CreateChain(1234, 5678);
        var result = BignumHelper.MultiplyDigit(x, 1);
        AssertChainEqual(x, result);
    }

    [Fact]
    public void MultiplyDigit_WithCarry_ShouldMultiplyCorrectly()
    {
        var x = CreateChain(5000, 2500); // 25005000
        // 5000 * 3 = 15000 -> 5000 (carry 1)
        // 2500 * 3 + 1 = 7501
        var expected = CreateChain(5000, 7501);

        var result = BignumHelper.MultiplyDigit(x, 3);

        AssertChainEqual(expected, result);
    }

    #endregion

    #region MultiplyRaw Tests

    [Fact]
    public void MultiplyRaw_MultiplyWithEmpty_ShouldReturnEmpty()
    {
        var x = CreateChain(1234);
        var result = BignumHelper.MultiplyRaw(x, NodeChain.Empty);
        Assert.True(result.IsEmpty);
    }

    [Fact]
    public void MultiplyRaw_ValidChains_ShouldMultiplyCorrectly()
    {
        var x = CreateChain(1234);
        var y = CreateChain(5678);
        // 1234 * 5678 = 7006652 -> 6652 -> 700
        var expected = CreateChain(6652, 700);

        var result = BignumHelper.MultiplyRaw(x, y);

        AssertChainEqual(expected, result);
    }

    #endregion

    #region DivideRaw Tests

    [Fact]
    public void DivideRaw_DivisorZero_ShouldThrow()
    {
        var x = CreateChain(1234);
        Assert.Throws<DivideByZeroException>(() => BignumHelper.DivideRaw(x, NodeChain.Empty));
    }

    [Fact]
    public void DivideRaw_DividendLessThanDivisor_ShouldReturnZeroAndDividend()
    {
        var x = CreateChain(1234);
        var y = CreateChain(5678);

        var (quotient, remainder) = BignumHelper.DivideRaw(x, y);

        Assert.True(quotient.IsEmpty);
        AssertChainEqual(x, remainder);
    }

    [Fact]
    public void DivideRaw_DividendEqualsDivisor_ShouldReturnOneAndZero()
    {
        var x = CreateChain(1234, 5678);

        var (quotient, remainder) = BignumHelper.DivideRaw(x, x);

        AssertChainEqual(CreateChain(1), quotient);
        Assert.True(remainder.IsEmpty);
    }

    [Fact]
    public void DivideRaw_SimpleDivision_ShouldDivideCorrectly()
    {
        var x = CreateChain(0, 1); // 10000
        var y = CreateChain(2); // 2
        var expectedQuot = CreateChain(5000); // 5000

        var (quotient, remainder) = BignumHelper.DivideRaw(x, y);

        AssertChainEqual(expectedQuot, quotient);
        Assert.True(remainder.IsEmpty);
    }

    #endregion

    #region CompareRaw Tests

    [Fact]
    public void CompareRaw_EqualChains_ShouldReturnZero()
    {
        var x = CreateChain(1234, 5678);
        var y = CreateChain(1234, 5678);
        Assert.Equal(0, BignumHelper.CompareRaw(x, y));
    }

    [Theory]
    [InlineData(new[] { 1234 }, new[] { 1234, 1 }, -1)] // x has fewer nodes
    [InlineData(new[] { 1234, 1 }, new[] { 1234 }, 1)] // x has more nodes
    [InlineData(new[] { 1234, 5678 }, new[] { 1234, 9999 }, -1)] // x most significant node is smaller
    [InlineData(new[] { 1234, 9999 }, new[] { 1234, 5678 }, 1)] // x most significant node is larger
    [InlineData(new[] { 1230, 5678 }, new[] { 1234, 5678 }, -1)] // x least significant node is smaller (same MSN)
    [InlineData(new[] { 1234, 5678 }, new[] { 1230, 5678 }, 1)] // x least significant node is larger (same MSN)
    public void CompareRaw_VariousChains_ShouldReturnExpectedComparison(int[] xVal, int[] yVal, int expected)
    {
        var x = CreateChain(xVal);
        var y = CreateChain(yVal);
        Assert.Equal(expected, BignumHelper.CompareRaw(x, y));
    }

    #endregion
}