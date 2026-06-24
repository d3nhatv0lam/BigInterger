using System;
using Bignum.Core.Bignum;
using Xunit;
using BignumClass = Bignum.Core.Bignum.Bignum;

namespace Bignum.Tests.Core.Bignum;

public class BignumTests
{
    #region Constructor Tests

    [Theory]
    [InlineData("0", false, 0)]
    [InlineData("1234", false, 1)]
    [InlineData("10000", false, 2)]
    [InlineData("-12345678", true, 2)]
    [InlineData("+12345678", false, 2)]
    [InlineData("000123", false, 1)]
    [InlineData("-00000", false, 0)] // negative zero becomes zero
    public void Constructor_ValidString_ShouldParseCorrectly(string input, bool expectedNegative, int expectedNodeCount)
    {
        var b = new BignumClass(input);

        Assert.Equal(expectedNegative, b.IsNegative);
        Assert.Equal(expectedNodeCount, b.NodeCount);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Constructor_NullOrWhitespace_ShouldThrowArgumentException(string? input)
    {
#pragma warning disable CS8604 // Possible null reference argument
        Assert.ThrowsAny<ArgumentException>(() => new BignumClass(input));
#pragma warning restore CS8604
    }

    [Theory]
    [InlineData("-")]
    [InlineData("+")]
    [InlineData("123a4")]
    [InlineData("12.34")]
    [InlineData("12 3")]
    public void Constructor_InvalidFormat_ShouldThrowFormatException(string input)
    {
        Assert.Throws<FormatException>(() => new BignumClass(input));
    }

    [Fact]
    public void Constructor_ExceedsMaxDigits_ShouldThrowArgumentOutOfRangeException()
    {
        // Max is 10000 digits. Let's create a string of 10001 digits.
        string input = new string('9', BignumConstants.NodeBase + 1);
        Assert.Throws<ArgumentOutOfRangeException>(() => new BignumClass(input));
    }

    [Fact]
    public void Constructor_LongValue_ShouldMatchStringConstructor()
    {
        long val = 1234567890L;
        var b1 = new BignumClass(val);
        var b2 = new BignumClass(val.ToString());

        Assert.Equal(b1.IsNegative, b2.IsNegative);
        Assert.Equal(b1.NodeCount, b2.NodeCount);
        Assert.Equal(b1.ToStringNumber(), b2.ToStringNumber());
    }

    #endregion

    #region ToString Tests

    [Theory]
    [InlineData("0", "0")]
    [InlineData("1", "1")]
    [InlineData("-1", "-1")]
    [InlineData("12345678", "12345678")]
    [InlineData("-12345678", "-12345678")]
    [InlineData("10000", "10000")]
    [InlineData("10002", "10002")]
    [InlineData("100000000", "100000000")]
    [InlineData("12345678901234567890", "12345678901234567890")]
    public void ToString_ShouldReturnCorrectString(string input, string expected)
    {
        var b = new BignumClass(input);
        Assert.Equal(expected, b.ToStringNumber());
    }

    #endregion

    #region Operator Tests

    [Theory]
    // Addition
    [InlineData("12", "34", "46", "+")]
    [InlineData("9999", "1", "10000", "+")]
    [InlineData("12345678", "87654322", "100000000", "+")]
    [InlineData("-12", "-34", "-46", "+")]
    [InlineData("-12", "34", "22", "+")]
    [InlineData("12", "-34", "-22", "+")]
    [InlineData("-9999", "1", "-9998", "+")]
    // Subtraction
    [InlineData("34", "12", "22", "-")]
    [InlineData("12", "34", "-22", "-")]
    [InlineData("-12", "-34", "22", "-")]
    [InlineData("-12", "34", "-46", "-")]
    [InlineData("12345678", "12345678", "0", "-")]
    // Multiplication
    [InlineData("12", "3", "36", "*")]
    [InlineData("-12", "3", "-36", "*")]
    [InlineData("-12", "-3", "36", "*")]
    [InlineData("0", "12345678", "0", "*")]
    [InlineData("12345678", "12345678", "152415765279684", "*")]
    // Division
    [InlineData("12", "3", "4", "/")]
    [InlineData("12", "-3", "-4", "/")]
    [InlineData("-12", "-3", "4", "/")]
    [InlineData("5", "2", "2", "/")]
    [InlineData("12345678", "10000", "1234", "/")]
    // Modulo
    [InlineData("12", "5", "2", "%")]
    [InlineData("12", "-5", "2", "%")] // Sign of remainder matches dividend in C#
    [InlineData("-12", "5", "-2", "%")]
    [InlineData("5", "10", "5", "%")]
    public void ArithmeticOperators_ShouldCalculateCorrectly(string leftStr, string rightStr, string expectedStr, string op)
    {
        var left = new BignumClass(leftStr);
        var right = new BignumClass(rightStr);
        BignumClass result = op switch
        {
            "+" => left + right,
            "-" => left - right,
            "*" => left * right,
            "/" => left / right,
            "%" => left % right,
            _ => throw new InvalidOperationException()
        };

        Assert.Equal(expectedStr, result.ToStringNumber());
    }

    [Fact]
    public void UnaryMinus_ShouldNegateCorrectly()
    {
        var positive = new BignumClass("12345678");
        var negative = -positive;
        Assert.Equal("-12345678", negative.ToStringNumber());

        var zero = new BignumClass("0");
        var negatedZero = -zero;
        Assert.Equal("0", negatedZero.ToStringNumber());
    }

    #endregion
}
