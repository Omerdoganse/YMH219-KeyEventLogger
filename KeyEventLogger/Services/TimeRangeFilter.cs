using KeyEventLogger.Models;

namespace KeyEventLogger.Services;

/// <summary>
/// Kayitlari tarih/saat araligina gore suzen filtre. Bos (inaktif) olabilir.
/// </summary>
public sealed class TimeRangeFilter
{
    public DateTime? From { get; private set; }
    public DateTime? To { get; private set; }

    public bool IsActive => From.HasValue || To.HasValue;

    /// <summary>Filtre araligini ayarlar. Baslangic bitisten sonra olamaz.</summary>
    public void Set(DateTime from, DateTime to)
    {
        if (from > to)
            throw new ArgumentException("Başlangıç zamanı bitiş zamanından sonra olamaz.");
        From = from;
        To = to;
    }

    public void Reset()
    {
        From = null;
        To = null;
    }

    public bool Matches(KeyLogEntry entry) =>
        (!From.HasValue || entry.Timestamp >= From.Value) &&
        (!To.HasValue || entry.Timestamp <= To.Value);

    public IEnumerable<KeyLogEntry> Apply(IEnumerable<KeyLogEntry> entries) =>
        IsActive ? entries.Where(Matches) : entries;

    public override string ToString() =>
        IsActive ? $"{From:dd.MM HH:mm:ss} - {To:dd.MM HH:mm:ss}" : "Filtre yok";
}
