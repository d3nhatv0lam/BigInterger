using System;
using System.Collections.Generic;
using Bignum.Core.Bignum;

namespace Bignum.Services;

public class SimStep
{
    public int StepIndex { get; set; }
    public int? NodeIndex { get; set; }
    public int ValueA { get; set; }
    public int ValueB { get; set; }
    public int CarryIn { get; set; }
    public int CarryOut { get; set; }
    public int ResultValue { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool IsFinished { get; set; }

    // Lưu trữ chuỗi kết quả trung gian dưới dạng mảng các Node để binding vẽ giao diện kết quả tại bước này
    public List<int> IntermediateResult { get; set; } = new();
}

public static class BignumSimulator
{
    public static List<SimStep> GenerateAddSteps(Bignum.Core.Bignum.Bignum x, Bignum.Core.Bignum.Bignum y)
    {
        var steps = new List<SimStep>();
        var currentX = x.Head;
        var currentY = y.Head;

        var stepIndex = 0;
        var nodeIndex = 0;
        var carry = 0;
        var currentResultList = new List<int>();

        while (currentX is not null || currentY is not null || carry > 0)
        {
            var valX = currentX?.Value ?? 0;
            var valY = currentY?.Value ?? 0;

            var sum = valX + valY + carry;
            var nextCarry = sum / BignumConstants.NodeBase;
            var resVal = sum % BignumConstants.NodeBase;

            currentResultList.Add(resVal);

            var desc = $"[Bước {stepIndex + 1}] Cộng hai Node ở hàng {nodeIndex}:\n" +
                       $"• Lấy giá trị Node A ({valX}) + Node B ({valY})";
            if (carry > 0) desc += $" + {carry} (nhớ)";
            desc += $" = {sum}.\n" +
                    $"• Kết quả Node mới = {sum} % {BignumConstants.NodeBase} = {resVal}.\n" +
                    $"• Số nhớ mang đi tiếp theo = {sum} / {BignumConstants.NodeBase} = {nextCarry}.";

            steps.Add(new SimStep
            {
                StepIndex = stepIndex++,
                NodeIndex = nodeIndex,
                ValueA = valX,
                ValueB = valY,
                CarryIn = carry,
                CarryOut = nextCarry,
                ResultValue = resVal,
                Description = desc,
                IntermediateResult = new List<int>(currentResultList)
            });

            carry = nextCarry;
            nodeIndex++;
            currentX = currentX?.Next;
            currentY = currentY?.Next;
        }

        steps.Add(new SimStep
        {
            StepIndex = stepIndex,
            IsFinished = true,
            Description = "Phép cộng đã hoàn tất! Danh sách liên kết kết quả đã được thiết lập đầy đủ.",
            IntermediateResult = new List<int>(currentResultList)
        });

        return steps;
    }

    public static List<SimStep> GenerateSubtractSteps(Bignum.Core.Bignum.Bignum minuend,
        Bignum.Core.Bignum.Bignum subtrahend)
    {
        var steps = new List<SimStep>();
        var currentM = minuend.Head;
        var currentS = subtrahend.Head;

        var stepIndex = 0;
        var nodeIndex = 0;
        var borrow = 0;
        var currentResultList = new List<int>();

        while (currentM is not null)
        {
            var valM = currentM.Value;
            var valS = currentS?.Value ?? 0;

            var diff = valM - valS - borrow;
            int nextBorrow;
            int resVal;

            var desc = $"[Bước {stepIndex + 1}] Trừ hai Node ở hàng {nodeIndex}:\n" +
                       $"• Lấy giá trị Node A ({valM}) - Node B ({valS})";
            if (borrow > 0) desc += $" - {borrow} (mượn)";

            if (diff < 0)
            {
                nextBorrow = 1;
                resVal = diff + BignumConstants.NodeBase;
                desc += $" = {diff} < 0.\n" +
                        $"• Do hiệu nhỏ hơn 0, ta mượn {BignumConstants.NodeBase} từ Node tiếp theo: {diff} + {BignumConstants.NodeBase} = {resVal}.\n" +
                        $"• Giá trị Node kết quả = {resVal}.\n" +
                        $"• Số mượn mang đi tiếp theo = 1.";
            }
            else
            {
                nextBorrow = 0;
                resVal = diff;
                desc += $" = {resVal}.\n" +
                        $"• Giá trị Node kết quả = {resVal}.\n" +
                        $"• Không có số mượn (số mượn mang đi tiếp theo = 0).";
            }

            currentResultList.Add(resVal);

            steps.Add(new SimStep
            {
                StepIndex = stepIndex++,
                NodeIndex = nodeIndex,
                ValueA = valM,
                ValueB = valS,
                CarryIn = borrow,
                CarryOut = nextBorrow,
                ResultValue = resVal,
                Description = desc,
                IntermediateResult = new List<int>(currentResultList)
            });

            borrow = nextBorrow;
            nodeIndex++;
            currentM = currentM.Next;
            currentS = currentS?.Next;
        }

        // Loại bỏ các node 0 vô nghĩa ở đuôi danh sách kết quả (chỉ giữ lại 1 node nếu kết quả là 0)
        while (currentResultList.Count > 1 && currentResultList[^1] == 0)
        {
            currentResultList.RemoveAt(currentResultList.Count - 1);
        }

        if (currentResultList.Count == 0) currentResultList.Add(0);

        steps.Add(new SimStep
        {
            StepIndex = stepIndex,
            IsFinished = true,
            Description =
                "Phép trừ đã hoàn tất! Các Node 0 vô nghĩa ở đuôi đã được loại bỏ và danh sách liên kết kết quả đã hoàn thành.",
            IntermediateResult = new List<int>(currentResultList)
        });

        return steps;
    }
}