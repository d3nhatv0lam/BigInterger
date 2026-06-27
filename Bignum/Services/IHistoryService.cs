using System;
using System.Collections.Generic;

namespace Bignum.Services;

public interface IHistoryService
{
    List<HistoryEntry> LoadHistory();
    void AddEntry(HistoryEntry entry);
    void ClearHistory();
}
