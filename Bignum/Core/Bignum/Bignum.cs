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

    public Bignum(int value) : this((long)value)
    {
    }

    public Bignum(long value)
    {
        IsNegative = value < 0;
        var absVal = Math.Abs(value);
        if (absVal == 0)
        {
            IsNegative = false;
            Head = null;
            Tail = null;
            NodeCount = 0;
            return;
        }

        var dummy = new BignumNode(0);
        var current = dummy;
        var count = 0;
        while (absVal > 0)
        {
            var nodeVal = (int)(absVal % BignumConstants.NodeBase);
            current.Next = new BignumNode(nodeVal);
            current = current.Next;
            count++;
            absVal /= BignumConstants.NodeBase;
        }

        Head = dummy.Next;
        NodeCount = count;
        InitTail();
    }

    public Bignum(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value.Length, BignumConstants.MaxDigitOfBignum);

        throw new NotImplementedException();
    }

    private Bignum(bool isNegative, BignumNode? head, int nodeCount)
    {
        IsNegative = isNegative;
        Head = head;
        NodeCount = nodeCount;
        InitTail();
    }

    private Bignum(bool isNegative, NodeChain chain)
        : this(isNegative, chain.Head, chain.NodeCount)
    {
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
            var isNeg = !resultChain.IsEmpty && x.IsNegative;
            return new Bignum(isNeg, resultChain);
        }

        // khác dấu
        if (BignumHelper.CompareRaw(xChain, yChain) >= 0)
        {
            resultChain = BignumHelper.SubtractRaw(xChain, yChain);
            var isNeg = !resultChain.IsEmpty && x.IsNegative;
            return new Bignum(isNeg, resultChain);
        }
        else
        {
            resultChain = BignumHelper.SubtractRaw(yChain, xChain);
            var isNeg = !resultChain.IsEmpty && y.IsNegative;
            return new Bignum(isNeg, resultChain);
        }
    }

    public static Bignum operator -(Bignum x)
    {
        ArgumentNullException.ThrowIfNull(x);
        if (x.Head is null) return x;
        return new Bignum(!x.IsNegative, x.Head, x.NodeCount);
    }

    public static Bignum operator -(Bignum x, Bignum y)
    {
        ArgumentNullException.ThrowIfNull(x);
        ArgumentNullException.ThrowIfNull(y);

        return x + -y;
    }

    public static Bignum operator *(Bignum x, Bignum y)
    {
        ArgumentNullException.ThrowIfNull(x);
        ArgumentNullException.ThrowIfNull(y);

        var xChain = new NodeChain(x.Head, x.NodeCount);
        var yChain = new NodeChain(y.Head, y.NodeCount);

        var resultChain = BignumHelper.MultiplyRaw(xChain, yChain);

        var isNegative = x.IsNegative ^ y.IsNegative;
        if (resultChain.IsEmpty) isNegative = false;
        return new Bignum(isNegative, resultChain.Head, resultChain.NodeCount);
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

        var xChain = new NodeChain(x.Head, x.NodeCount);
        var yChain = new NodeChain(y.Head, y.NodeCount);
        var (quotChain, remChain) = BignumHelper.DivideRaw(xChain, yChain);

        var isQuotNegative = x.IsNegative ^ y.IsNegative;
        if (quotChain.IsEmpty) isQuotNegative = false;
        var isRemNegative = x.IsNegative;
        if (remChain.IsEmpty) isRemNegative = false;

        return (
            new Bignum(isQuotNegative, quotChain.Head, quotChain.NodeCount),
            new Bignum(isRemNegative, remChain.Head, remChain.NodeCount)
        );
    }

    public override string ToString()
    {
        if (Head is null) return "0";

        var builder = new StringBuilder();
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