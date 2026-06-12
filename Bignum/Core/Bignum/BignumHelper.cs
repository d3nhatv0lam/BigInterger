namespace Bignum.Core.Bignum;

public static class BignumHelper
{
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
}