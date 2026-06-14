using System;

namespace Bignum.Core.Bignum;

public static class BignumHelper
{
    /// <summary>
    /// |X| + |Y|
    /// </summary>
    /// <param name="headX"></param>
    /// <param name="countX"></param>
    /// <param name="headY"></param>
    /// <param name="countY"></param>
    /// <returns></returns>
    public static BignumNode? AddRaw(BignumNode? headX, int countX, BignumNode? headY, int countY)
    {
        if (headX is null) return CloneList(headY);
        if (headY is null) return CloneList(headX);

        var currentX = headX;
        var currentY = headY;
        
        BignumNode dummy = new BignumNode(0);
        BignumNode current = dummy;
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
            
            currentX = currentX?.Next;
            currentY = currentY?.Next;
        }
        
        return dummy.Next;
    }
    
    public static BignumNode? SubtractRaw(BignumNode? headX, int countX, BignumNode? headY, int countY)
    {
        throw new NotImplementedException();
    }
    
    public static BignumNode? MultiplyRaw(BignumNode? headX, int countX, BignumNode? headY, int countY)
    {
        throw new NotImplementedException();
    }
    
    public static (BignumNode? Quotient, BignumNode? Remainder) DivideRaw(BignumNode? headX, int countX, BignumNode? headY, int countY)
    {
        throw new NotImplementedException();
    }
    
    /// <summary>
    /// Compare |X| and |Y|
    /// </summary>
    /// <param name="headX"></param>
    /// <param name="countX"></param>
    /// <param name="headY"></param>
    /// <param name="countY"></param>
    /// <returns></returns>
    public static int CompareRaw(BignumNode? headX, int countX, BignumNode? headY, int countY)
    {
        if (countX < countY) return -1;
        if (countX > countY) return 1;

        // độ dài bằng nhau, tạo mảng để so sánh ngược từ Tail xuống Head
        var arrX = new int[countX];
        var arrY = new int[countY];

        int idx = 0;
        while (headX is not null && headY is not null)
        {
            arrX[idx] = headX.Value;
            arrY[idx] = headY.Value;
            headX = headX.Next;
            headY = headY.Next;
            idx++;
        }

        for (var i = countX - 1; i >= 0; i--)
        {
            if (arrX[i] < arrY[i]) return -1;
            if (arrX[i] > arrY[i]) return 1;
        }

        return 0;
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