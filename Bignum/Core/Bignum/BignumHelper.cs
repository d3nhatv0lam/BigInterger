using System;
using System.Buffers;

namespace Bignum.Core.Bignum;

public readonly record struct NodeChain(BignumNode? Head, int NodeCount)
{
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
        if (x.Head is null) return y with { Head = CloneList(x.Head) };
        if (y.Head is null) return x with { Head = CloneList(y.Head) };

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
                return new NodeChain(null, 0);
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
    /// |X| * |Y|
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public static NodeChain MultiplyRaw(NodeChain x, NodeChain y)
    {
        if (x.IsEmpty || y.IsEmpty) return new NodeChain(null, 0);

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
                        currentSum =  buffer[productIndex] + carry;
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


            var resCount = 0;
            for (var i = 0; i < requiredLength; i++)
            {
                resCount++;
                dummy.Next = new BignumNode(buffer[i]);
                dummy = dummy.Next;
            }

            var rawResult = new NodeChain(dummy.Next, resCount);

            return CleanTail(rawResult);
        }
        finally
        {
            ArrayPool<int>.Shared.Return(rentedResult);
        }
    }

    public static (NodeChain Quotient, NodeChain Remainder) DivideRaw(NodeChain dividend, NodeChain divisor)
    {
        throw new NotImplementedException();
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

        if (lastNonZeroNode is null) return new NodeChain(null, 0);

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

        Span<int> bufferX = count <= 256 ? stackalloc int[count] : (rentedX = ArrayPool<int>.Shared.Rent(count));
        Span<int> bufferY = count <= 256 ? stackalloc int[count] : (rentedY = ArrayPool<int>.Shared.Rent(count));

        try
        {
            var currentX = x.Head;
            var currentY = y.Head;
            int idx = 0;

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

    private static BignumNode? CloneList(BignumNode? head)
    {
        if (head is null) return null;

        BignumNode dummy = new BignumNode(0);
        BignumNode current = dummy;
        BignumNode? walker = head;

        while (walker is not null)
        {
            current.Next = new BignumNode(walker.Value);
            current = current.Next;
            walker = walker.Next;
        }

        return dummy.Next;
    }
}