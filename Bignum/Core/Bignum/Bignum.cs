using System;
using System.Collections.Generic;
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
        value = value.Trim();

        var isNegative = false;
        var startIndex = 0;

        switch (value[0])
        {
            case '-':
                isNegative = true;
                startIndex = 1;
                break;
            case '+':
                startIndex = 1;
                break;
        }
        
        // chỉ có dấu
        if (startIndex >= value.Length)
        {
            throw new FormatException("Input string was not in a correct format.");
        }
        
        var digitCount = value.Length - startIndex;
        if (digitCount > BignumConstants.MaxDigitOfBignum)
        {
            throw new ArgumentOutOfRangeException(nameof(value), $"Number of digits exceeds maximum allowed ({BignumConstants.MaxDigitOfBignum}).");
        }

        for (var i = startIndex; i < value.Length; i++)
        {
            if (!char.IsDigit(value[i])) throw new FormatException("Input string was not in a correct format.");
        }
        
        // cắt số 0 ở đầu
        while (startIndex < value.Length && value[startIndex] == '0')
        {
            startIndex++;
        }

        // node 0
        if (startIndex >= value.Length)
        {
            Head = null;
            Tail = null;
            NodeCount = 0;
            return;
        }
        
        this.IsNegative = isNegative;
        
        var dummy = new BignumNode(0);
        var current = dummy;
        var count = 0;
        
        var end = value.Length;
        while (startIndex < end)
        {
            var start = Math.Max(startIndex, end - BignumConstants.NodeDigitCount);
            var blockStr = value.Substring(start, end - start);
            var nodeParse = int.Parse(blockStr);
            
            current.Next = new BignumNode(nodeParse);
            current = current.Next;
            count++;
            
            end -= BignumConstants.NodeDigitCount;
        }
        
        Head = dummy.Next;
        NodeCount = count;
        InitTail();
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

        var parts = new List<string>(NodeCount);
        var current = Head;

        while (current is not null)
        {
            parts.Add(current.Next is null
                ? current.Value.ToString()
                : current.Value.ToString(BignumConstants.BignumFormat));

            current = current.Next;
        }

        parts.Reverse();
        
        var sign = IsNegative ? "-" : "";
        return sign + string.Concat(parts);
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
}