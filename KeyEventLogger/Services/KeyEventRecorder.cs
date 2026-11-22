using KeyEventLogger.Models;

namespace KeyEventLogger.Services;

/// <summary>
/// Kayit durumunu ve kayit listesini yoneten servis.
/// Form yalnizca olaylari bu sinifa iletir; kayit kurallari burada tutulur.
/// </summary>
public class KeyEventRecorder
{
    private readonly List<KeyLogEntry> _entries = new();

    /// <summary>Bellekte tutulacak en fazla kayit sayisi (cok uzun oturumlarda bellek sismesin).</summary>
    public int MaxEntries { get; }

    public bool IsRecording { get; private set; }

    /// <summary>Disaridan yalnizca okunabilir kayit listesi.</summary>
    public IReadOnlyList<KeyLogEntry> Entries => _entries;

    /// <summary>Hangi olay turlerinin kaydedilecegi.</summary>
    public HashSet<KeyEventType> EnabledTypes { get; } =
        new() { KeyEventType.KeyDown, KeyEventType.KeyUp, KeyEventType.KeyPress };

    public event EventHandler<KeyLogEntry>? EntryAdded;
    public event EventHandler? EntriesCleared;
    public event EventHandler<bool>? RecordingStateChanged;

    public KeyEventRecorder(int maxEntries = 10000)
    {
        if (maxEntries <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxEntries), "Kayit siniri pozitif olmalidir.");
        MaxEntries = maxEntries;
    }

    public void Start()
    {
        if (IsRecording) return;
        IsRecording = true;
        RecordingStateChanged?.Invoke(this, true);
    }

    public void Stop()
    {
        if (!IsRecording) return;
        IsRecording = false;
        RecordingStateChanged?.Invoke(this, false);
    }

    /// <summary>
    /// Kayit aciksa ve olay turu etkinse kaydi ekler. Eklenirse true dondurur.
    /// </summary>
    public bool Record(KeyLogEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        if (!IsRecording || !EnabledTypes.Contains(entry.EventType))
            return false;

        if (_entries.Count >= MaxEntries)
            _entries.RemoveAt(0);

        _entries.Add(entry);
        EntryAdded?.Invoke(this, entry);
        return true;
    }

    public void Clear()
    {
        _entries.Clear();
        EntriesCleared?.Invoke(this, EventArgs.Empty);
    }
}
