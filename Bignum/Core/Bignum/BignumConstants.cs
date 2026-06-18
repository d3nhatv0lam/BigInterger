using System;

namespace Bignum.Core.Bignum;

public static class BignumConstants
{
    // value : 0 ~ 9999
    public const int NodeDigitCount = 4;
    /// <summary>
    /// Số chữ số tối đa cho phép tạo thành bignum
    /// </summary>
    public const int MaxDigitOfBignum = 10000;

    public static readonly int NodeBase = NodeDigitCount switch
    {
        1 => 10,
        2 => 100,
        3 => 1_000,
        4 => 10_000,
        5 => 100_000,
        6 => 1_000_000,
        _ => throw new ArgumentOutOfRangeException(nameof(NodeDigitCount))
    };

    public const int NodeMinValue = 0;
    public static readonly int NodeMaxValue = NodeBase - 1;
    public static readonly string BignumFormat = "D" + NodeDigitCount;
}