using System;
using System.Buffers;
using DynamicData;

namespace Bignum.Core.Bignum;

public readonly record struct NodeChain(BignumNode? Head, int NodeCount)
{
    public static readonly NodeChain Empty = new(null, 0);
    public bool IsEmpty => Head is null || NodeCount == 0;
}

public static class BignumHelper
{
    /// <summary>
    /// |X| + |Y|
    /// </summary>
    /// <returns></returns>
    public static NodeChain AddRaw(NodeChain x, NodeChain y)
    {
        if (x.Head is null) return y;
        if (y.Head is null) return x;

        var currentX = x.Head;
        var currentY = y.Head;

        BignumNode dummy = new BignumNode(0);
        BignumNode current = dummy;
        var resultCount = 0;
        var carry = 0;

        while (currentX is not null || currentY is not null || carry > 0)
        {
            var xValue = currentX?.Value ?? 0;
            var yValue = currentY?.Value ?? 0;

            var sum = xValue + yValue + carry;
            carry = sum / BignumConstants.NodeBase;

            var node = new BignumNode(sum % BignumConstants.NodeBase);
            current.Next = node;
            current = node;

            resultCount++;

            currentX = currentX?.Next;
            currentY = currentY?.Next;
        }

        return new NodeChain(dummy.Next, resultCount);
    }

    /// <summary>
    /// |minuend| - |subtrahend|, điều kiện (|minuend| >= |subtrahend|)
    /// </summary>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public static NodeChain SubtractRaw(NodeChain minuend, NodeChain subtrahend)
    {
        var compareResult = CompareRaw(minuend, subtrahend);

        switch (compareResult)
        {
            case < 0:
                throw new InvalidOperationException("Minuend must be greater than or equal to subtrahend.");
            case 0:
                return NodeChain.Empty;
        }

        var dummy = new BignumNode(0);
        var current = dummy;
        var currentMinuend = minuend.Head;
        var currentSubtrahend = subtrahend.Head;
        var resultCount = 0;
        var carry = 0;

        while (currentMinuend is not null)
        {
            var minuendValue = currentMinuend.Value;
            var subtrahendValue = currentSubtrahend?.Value ?? 0;

            var difference = minuendValue - subtrahendValue - carry;

            if (difference < 0)
            {
                difference += BignumConstants.NodeBase;
                carry = 1;
            }
            else carry = 0;

            var node = new BignumNode(difference);
            current.Next = node;
            current = node;

            resultCount++;

            currentMinuend = currentMinuend.Next;
            currentSubtrahend = currentSubtrahend?.Next;
        }

        var rawResult = new NodeChain(dummy.Next, resultCount);

        return CleanTail(rawResult);
    }

    /// <summary>
    /// |X| * (int) y
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    public static NodeChain MultiplyDigit(NodeChain x, int y)
    {
        if (x.IsEmpty || y == 0) return NodeChain.Empty;
        if (y == 1) return x;

        var dummy = new BignumNode(0);
        var current = dummy;
        var currentX = x.Head;
        var resultCount = 0;
        var carry = 0;

        while (currentX is not null || carry > 0)
        {
            var sum = (long)(currentX?.Value ?? 0) * y + carry;
            var node = new BignumNode((int)(sum % BignumConstants.NodeBase));
            current.Next = node;
            current = current.Next;
            carry = (int)(sum / BignumConstants.NodeBase);
            resultCount++;

            currentX = currentX?.Next;
        }

        return CleanTail(new NodeChain(dummy.Next, resultCount));
    }

    /// <summary>
    /// |X| * |Y|
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public static NodeChain MultiplyRaw(NodeChain x, NodeChain y)
    {
        if (x.IsEmpty || y.IsEmpty) return NodeChain.Empty;

        var dummy = new BignumNode(0);

        var requiredLength = x.NodeCount + y.NodeCount;
        var rentedResult = ArrayPool<int>.Shared.Rent(requiredLength);
        Array.Clear(rentedResult);
        var buffer = rentedResult.AsSpan(0, requiredLength);

        try
        {
            var xIndex = 0;
            var currentX = x.Head;

            while (currentX is not null)
            {
                var yIndex = 0;
                var currentY = y.Head;
                while (currentY is not null)
                {
                    var product = (long)currentX.Value * currentY.Value;
                    var productIndex = xIndex + yIndex;

                    var currentSum = buffer[productIndex] + product;
                    buffer[productIndex] = (int)(currentSum % BignumConstants.NodeBase);
                    var carry = currentSum / BignumConstants.NodeBase;

                    productIndex++;
                    while (carry > 0)
                    {
                        currentSum = buffer[productIndex] + carry;
                        buffer[productIndex] = (int)(currentSum % BignumConstants.NodeBase);
                        carry = currentSum / BignumConstants.NodeBase;

                        productIndex++;
                    }

                    yIndex++;
                    currentY = currentY.Next;
                }

                xIndex++;
                currentX = currentX.Next;
            }


            var current = dummy;
            var resCount = 0;
            for (var i = 0; i < requiredLength; i++)
            {
                resCount++;
                current.Next = new BignumNode(buffer[i]);
                current = current.Next;
            }

            var rawResult = new NodeChain(dummy.Next, resCount);

            return CleanTail(rawResult);
        }
        finally
        {
            ArrayPool<int>.Shared.Return(rentedResult);
        }
    }

    /// <summary>
    /// |dividend| / |divisor| = (Quotient, Remainder) (|B| != 0)
    /// </summary>
    /// <param name="dividend"></param>
    /// <param name="divisor"></param>
    /// <returns></returns>
    /// <exception cref="DivideByZeroException"></exception>
    /// <exception cref="NotImplementedException"></exception>
    public static (NodeChain Quotient, NodeChain Remainder) DivideRaw(NodeChain dividend, NodeChain divisor)
    {
        if (divisor.IsEmpty) throw new DivideByZeroException("Divisor cannot be zero.");

        var compareResult = CompareRaw(dividend, divisor);
        switch (compareResult)
        {
            case < 0:
                return (NodeChain.Empty, dividend);
            case 0:
                return (
                    new NodeChain(new BignumNode(1), 1),
                    NodeChain.Empty);
        }
        
        var dividendNodes = new int[dividend.NodeCount];
        var temp = dividend.Head;
        for (var i = 0; i < dividend.NodeCount; i++)
        {
            dividendNodes[dividend.NodeCount - 1 - i] = temp!.Value;
            temp = temp.Next;
        }

        var quotientNodes = new int[dividend.NodeCount];
        var remainder = NodeChain.Empty;

        for (var i = 0; i < dividend.NodeCount; i++)
        {
            var newHead = new BignumNode(dividendNodes[i], remainder.Head);
            remainder = new NodeChain(newHead, remainder.NodeCount + 1);
            remainder = CleanTail(remainder);
            
            // A = B * q + r => có B,r. Tìm q sao cho biểu thức gần A nhất có thể 
            var q = FindQuotientDigit(divisor, remainder);
            
            // vì gần nhất có thể, nên A >= A_founded, phải trừ lại để làm remainder mới 
            var sub = MultiplyDigit(divisor, q);
            remainder = SubtractRaw(remainder, sub);
            
            quotientNodes[i] = q;
        }

        var dummy = new BignumNode(0);
        var current = dummy;
        for (var i = dividend.NodeCount - 1; i >= 0; i--)
        {
            var node = new BignumNode(quotientNodes[i]);
            current.Next = node;
            current = node;
        }
        
        var quotient = new NodeChain(dummy.Next, dividend.NodeCount);
        quotient = CleanTail(quotient);
        
        return (quotient, remainder);
        
        static int FindQuotientDigit(NodeChain div, NodeChain rem)
        {
            var low = 0;
            var high = BignumConstants.NodeMaxValue;
            var q = 0;
            while (low <= high)
            {
                var mid = low + (high - low) / 2;
                var product = MultiplyDigit(div, mid);
                if (CompareRaw(product, rem) <= 0)
                {
                    q = mid;
                    low = mid + 1;
                }
                else
                {
                    high = mid - 1;
                }
            }
            return q;
        }
    }

    /// <summary>
    /// loại bỏ số 0 vô nghĩa ở đuôi
    /// </summary>
    /// <param name="chain"></param>
    /// <returns></returns>
    private static NodeChain CleanTail(NodeChain chain)
    {
        if (chain.IsEmpty) return chain;

        BignumNode? current = chain.Head;
        BignumNode? lastNonZeroNode = null;

        int currentPosition = 0;
        int lastNonZeroPosition = 0;

        while (current is not null)
        {
            currentPosition++;
            if (current.Value != 0)
            {
                lastNonZeroNode = current;
                lastNonZeroPosition = currentPosition;
            }

            current = current.Next;
        }

        if (lastNonZeroNode is null) return NodeChain.Empty;

        lastNonZeroNode.Next = null;

        return new NodeChain(chain.Head, lastNonZeroPosition);
    }

    public static int CompareRaw(NodeChain x, NodeChain y)
    {
        if (x.NodeCount < y.NodeCount) return -1;
        if (x.NodeCount > y.NodeCount) return 1;
        if (x.IsEmpty) return 0;

        int count = x.NodeCount;

        // số Node nhỏ (<= 256), dùng Stackalloc 
        // số lượng Node lớn (> 256), mượn mảng từ ArrayPool tránh tràn Stack
        int[]? rentedX = null;
        int[]? rentedY = null;

        var bufferX = count <= 256 ? stackalloc int[count] : (rentedX = ArrayPool<int>.Shared.Rent(count));
        var bufferY = count <= 256 ? stackalloc int[count] : (rentedY = ArrayPool<int>.Shared.Rent(count));

        try
        {
            var currentX = x.Head;
            var currentY = y.Head;
            var idx = 0;

            while (currentX is not null && currentY is not null)
            {
                bufferX[idx] = currentX.Value;
                bufferY[idx] = currentY.Value;
                currentX = currentX.Next;
                currentY = currentY.Next;
                idx++;
            }

            // 4. Duyệt ngược từ đuôi về đầu
            for (int i = count - 1; i >= 0; i--)
            {
                if (bufferX[i] < bufferY[i]) return -1;
                if (bufferX[i] > bufferY[i]) return 1;
            }

            return 0;
        }
        finally
        {
            // Trả lại mảng cho Pool
            if (rentedX is not null) ArrayPool<int>.Shared.Return(rentedX);
            if (rentedY is not null) ArrayPool<int>.Shared.Return(rentedY);
        }
    }

}