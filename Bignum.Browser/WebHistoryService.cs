using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.InteropServices.JavaScript;
using System.Text.Json;
using System.Text.Json.Serialization;
using Bignum.Services;

namespace Bignum.Browser;

[JsonSerializable(typeof(List<HistoryEntry>))]
internal partial class HistoryJsonContext : JsonSerializerContext
{
}

public partial class WebHistoryService : IHistoryService
{
    private const string StorageKey = "bignum_calculation_history";

    [JSImport("globalThis.localStorage.setItem")]
    private static partial void SetLocalStorageItem(string key, string value);

    [JSImport("globalThis.localStorage.getItem")]
    private static partial string? GetLocalStorageItem(string key);

    [JSImport("globalThis.localStorage.removeItem")]
    private static partial void RemoveLocalStorageItem(string key);

    public List<HistoryEntry> LoadHistory()
    {
        try
        {
            string? json = GetLocalStorageItem(StorageKey);
            if (string.IsNullOrWhiteSpace(json))
            {
                return [];
            }

            return JsonSerializer.Deserialize(json, HistoryJsonContext.Default.ListHistoryEntry) ?? [];
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading history from LocalStorage: {ex.Message}");
            return [];
        }
    }

    public void AddEntry(HistoryEntry entry)
    {
        try
        {
            var list = LoadHistory();
            list.Add(entry);
            string json = JsonSerializer.Serialize(list, HistoryJsonContext.Default.ListHistoryEntry);
            SetLocalStorageItem(StorageKey, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error adding history entry to LocalStorage: {ex.Message}");
        }
    }

    public void ClearHistory()
    {
        try
        {
            RemoveLocalStorageItem(StorageKey);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error clearing history from LocalStorage: {ex.Message}");
        }
    }
}