using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Bignum.Services;

public class HistoryService : IHistoryService
{
    private readonly string _filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "history.json");
    private readonly Lock _fileLock = new();

    public List<HistoryEntry> LoadHistory()
    {
        lock (_fileLock)
        {
            try
            {
                if (!File.Exists(_filePath))
                {
                    return [];
                }

                string json = File.ReadAllText(_filePath);
                if (string.IsNullOrWhiteSpace(json))
                {
                    return [];
                }

                return JsonSerializer.Deserialize<List<HistoryEntry>>(json) ?? [];
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Lỗi khi đọc file lịch sử: {ex.Message}");
                return [];
            }
        }
    }

    public void AddEntry(HistoryEntry entry)
    {
        Task.Run(() =>
        {
            lock (_fileLock)
            {
                try
                {
                    var list = LoadHistory();
                    list.Add(entry);

                    var options = new JsonSerializerOptions { WriteIndented = true };
                    string json = JsonSerializer.Serialize(list, options);

                    File.WriteAllText(_filePath, json);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Lỗi khi ghi file lịch sử: {ex.Message}");
                }
            }
        });
    }

    public void ClearHistory()
    {
        lock (_fileLock)
        {
            try
            {
                if (File.Exists(_filePath))
                {
                    File.Delete(_filePath);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Lỗi khi xóa file lịch sử: {ex.Message}");
            }
        }
    }
}