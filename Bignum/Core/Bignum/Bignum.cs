using System;
using System.Text;

namespace Bignum.Core.Bignum;

/// <summary>
/// Bignum dạng 00001 -> number: 10000 
/// </summary>
public class Bignum
{
    public bool IsNegative { get; private set; }
    public BignumNode? Head { get; private set; }
    public BignumNode? Tail { get; private set; }
    public int NodeCount { get; private set; }

    public Bignum(bool isNegative)
    {
        IsNegative = isNegative;

        NodeCount = GetNodeCount();
    }

    public Bignum(int value)
    {
        IsNegative = value < 0;
    }

    public Bignum(long value)
    {
        IsNegative = value < 0;
        
    }

    public Bignum(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value.Length, BignumConstants.MaxDigitOfBignum);
    }

    private Bignum(bool isNegative, BignumNode? head, int nodeCount)
    {
        IsNegative = isNegative;
        Head = head;
        NodeCount = nodeCount;
        InitTail();
    }

    public static Bignum operator +(Bignum x, Bignum y)
    {
        ArgumentNullException.ThrowIfNull(x);
        ArgumentNullException.ThrowIfNull(y);

        var xChain = new NodeChain(x.Head, x.NodeCount);
        var yChain = new NodeChain(y.Head, y.NodeCount);
        NodeChain resultChain;
        // cùng dấu
        if (x.IsNegative == y.IsNegative)
        {
            resultChain = BignumHelper.AddRaw(xChain, yChain);
            return new Bignum(x.IsNegative, resultChain.Head, resultChain.NodeCount);
        }

        // khác dấu
        if (BignumHelper.CompareRaw(xChain, yChain) >= 0)
        {
            resultChain = BignumHelper.SubtractRaw(xChain, yChain);
            return new Bignum(x.IsNegative, resultChain.Head, resultChain.NodeCount);
        }
        else
        {
            resultChain = BignumHelper.SubtractRaw(yChain, xChain);
            return new Bignum(y.IsNegative, resultChain.Head, resultChain.NodeCount);
        }
    }

    public static Bignum operator -(Bignum x, Bignum y)
    {
        ArgumentNullException.ThrowIfNull(x);
        ArgumentNullException.ThrowIfNull(y);


        throw new NotImplementedException();
    }

    public static Bignum operator *(Bignum x, Bignum y)
    {
        ArgumentNullException.ThrowIfNull(x);
        ArgumentNullException.ThrowIfNull(y);


        throw new NotImplementedException();
    }

    public static Bignum operator /(Bignum x, Bignum y)
    {
        ArgumentNullException.ThrowIfNull(x);
        ArgumentNullException.ThrowIfNull(y);

        return Divide(x, y).Quotient;
    }

    public static Bignum operator %(Bignum x, Bignum y)
    {
        ArgumentNullException.ThrowIfNull(x);
        ArgumentNullException.ThrowIfNull(y);

        return Divide(x, y).Remainder;
    }

    public static (Bignum Quotient, Bignum Remainder) Divide(Bignum x, Bignum y)
    {
        ArgumentNullException.ThrowIfNull(x);
        ArgumentNullException.ThrowIfNull(y);


        throw new NotImplementedException();
    }

    public override string ToString()
    {
        if (Head is null) return "0";

        StringBuilder builder = new StringBuilder();
        var current = Head;

        while (current is not null)
        {
            if (current.Next is null)
            {
                builder.Append(current.Value);
            }
            else
            {
                builder.Append(current.Value.ToString(BignumConstants.BignumFormat));
            }

            current = current.Next;
        }

        return InPlaceReverse(builder);
    }

    private int GetNodeCount()
    {
        if (Head is null) return 0;

        var count = 0;
        var node = Head;
        while (node is not null)
        {
            node = node.Next;
            count++;
        }

        return count;
    }

    private void InitTail()
    {
        var current = Head;
        while (current?.Next is not null)
        {
            current = current.Next;
        }

        this.Tail = current;
    }

    private static string InPlaceReverse(StringBuilder sb)
    {
        int startIndex = 0;
        int endIndex = sb.Length - 1;

        while (startIndex < endIndex)
        {
            (sb[startIndex], sb[endIndex]) = (sb[endIndex], sb[startIndex]);

            startIndex++;
            endIndex--;
        }

        return sb.ToString();
    }
}