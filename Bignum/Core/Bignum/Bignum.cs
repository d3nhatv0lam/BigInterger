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

    public Bignum(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
    }

    public static Bignum operator +(Bignum x, Bignum y)
    {
        ArgumentNullException.ThrowIfNull(x);
        ArgumentNullException.ThrowIfNull(y);
        
        
        throw new NotImplementedException();
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
        var a = new Bignum(true);
        var b = new Bignum(false);

        var x = a + b;
        
        if (Head is null) return "";

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

    private string InPlaceReverse(StringBuilder sb)
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