using System.Text.Json.Serialization;

namespace Bignum.Services;

public class HistoryEntry
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("timestamp")]
    public string Timestamp { get; set; } = string.Empty;

    [JsonPropertyName("operation")]
    public string Operation { get; set; } = string.Empty;

    [JsonPropertyName("operand_1")]
    public string Operand1 { get; set; } = string.Empty;

    [JsonPropertyName("operand_2")]
    public string Operand2 { get; set; } = string.Empty;

    [JsonPropertyName("result_1")]
    public string Result1 { get; set; } = string.Empty;

    [JsonPropertyName("result_2")]
    public string? Result2 { get; set; }

    [JsonIgnore]
    public string OperationDisplayName => Operation switch
    {
        "Addition" => "Cộng (+)",
        "Subtraction" => "Trừ (-)",
        "Multiply" => "Nhân (x)",
        "Division" => "Chia (/)",
        _ => Operation
    };
}
