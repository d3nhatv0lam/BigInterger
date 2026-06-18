using System;

namespace Bignum.Core.Bignum;

public class BignumNode
{
    public int Value { get; init; }
    public BignumNode? Next { get; set; }

    public BignumNode(int value)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(value, BignumConstants.NodeMinValue);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value, BignumConstants.NodeMaxValue);
        Value = value;
    }

    public BignumNode(int value, BignumNode? next) : this(value)
    {
        Next = next;
    }
}