using Bignum.Core.Bignum;
using Bignum.Services;
using BignumClass = Bignum.Core.Bignum.Bignum;

namespace Bignum.Tests.Core.Bignum;

public class BignumSimulatorTests
{
    [Theory]
    [InlineData("12345678", "87654321")]
    [InlineData("99999999", "1")]
    [InlineData("0", "123456")]
    [InlineData("123456", "0")]
    public void GenerateAddSteps_ShouldMatchActualSum(string aStr, string bStr)
    {
        var a = new BignumClass(aStr);
        var b = new BignumClass(bStr);

        var actualSum = a + b;
        var steps = BignumSimulator.GenerateAddSteps(a, b);

        Assert.NotEmpty(steps);
        Assert.True(steps.Last().IsFinished);

        // Ghép các node từ bước trung gian cuối cùng để tạo thành kết quả string
        var finalStep = steps.Last();
        var reconstructedBignum = ReconstructBignum(finalStep.IntermediateResult, actualSum.IsNegative);

        Assert.Equal(actualSum.ToStringNumber(), reconstructedBignum.ToStringNumber());
    }

    [Theory]
    [InlineData("87654321", "12345678")]
    [InlineData("99999999", "1")]
    [InlineData("10000000", "9999999")]
    [InlineData("123456", "123456")]
    public void GenerateSubtractSteps_ShouldMatchActualDifference(string aStr, string bStr)
    {
        // a >= b để đảm bảo điều kiện của SubtractRaw
        var a = new BignumClass(aStr);
        var b = new BignumClass(bStr);

        var actualDiff = a - b;
        var steps = BignumSimulator.GenerateSubtractSteps(a, b);

        Assert.NotEmpty(steps);
        Assert.True(steps.Last().IsFinished);

        var finalStep = steps.Last();
        var reconstructedBignum = ReconstructBignum(finalStep.IntermediateResult, actualDiff.IsNegative);

        Assert.Equal(actualDiff.ToStringNumber(), reconstructedBignum.ToStringNumber());
    }

    private static BignumClass ReconstructBignum(List<int> nodes, bool isNegative)
    {
        if (nodes == null || nodes.Count == 0) return new BignumClass(0);

        // Vì nodes được lưu theo thứ tự Head -> Tail, ta chỉ cần tạo chuỗi ký tự theo thứ tự ngược lại (Tail -> Head)
        var parts = nodes.Select((val, idx) => 
            idx == nodes.Count - 1 
                ? val.ToString() 
                : val.ToString(BignumConstants.BignumFormat)
        ).Reverse();

        var numberStr = (isNegative ? "-" : "") + string.Concat(parts);
        if (string.IsNullOrEmpty(numberStr) || numberStr == "-") numberStr = "0";
        return new BignumClass(numberStr);
    }
}
