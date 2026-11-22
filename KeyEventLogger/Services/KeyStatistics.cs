using KeyEventLogger.Models;

namespace KeyEventLogger.Services;

/// <summary>Bir tusun kac kez basildigini tutan kucuk veri sinifi.</summary>
public sealed record KeyCount(string KeyName, int Count);

/// <summary>Istatistik hesaplamasinin sonucu.</summary>
public sealed record StatisticsResult(
    int Total,
    int KeyDownCount,
    int KeyUpCount,
    int KeyPressCount,
    IReadOnlyList<KeyCount> TopKeys);

/// <summary>
/// Kayit listesi uzerinden toplam tus sayisi ve en sik kullanilan tuslari hesaplar.
/// </summary>
public class KeyStatistics
{
    public int TopCount { get; }

    public KeyStatistics(int topCount = 5)
    {
        if (topCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(topCount), "En az 1 tus gosterilmelidir.");
        TopCount = topCount;
    }

    public StatisticsResult Calculate(IEnumerable<KeyLogEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);
        var list = entries as IReadOnlyCollection<KeyLogEntry> ?? entries.ToList();

        int down = list.Count(e => e.EventType == KeyEventType.KeyDown);
        int up = list.Count(e => e.EventType == KeyEventType.KeyUp);
        int press = list.Count(e => e.EventType == KeyEventType.KeyPress);

        // En sik kullanilan tuslar fiziksel basislardan (KeyDown) hesaplanir.
        var top = list
            .Where(e => e.EventType == KeyEventType.KeyDown)
            .GroupBy(e => e.KeyName)
            .Select(g => new KeyCount(g.Key, g.Count()))
            .OrderByDescending(k => k.Count)
            .ThenBy(k => k.KeyName, StringComparer.Ordinal)
            .Take(TopCount)
            .ToList();

        return new StatisticsResult(list.Count, down, up, press, top);
    }
}
