using System.Globalization;

namespace KeyEventLogger.Models;

/// <summary>
/// Tek bir klavye olayini temsil eden degismez (immutable) kayit sinifi.
/// </summary>
public sealed class KeyLogEntry
{
    public DateTime Timestamp { get; }
    public KeyEventType EventType { get; }

    /// <summary>Tusun okunabilir adi (ornek: "A", "Enter", "Space").</summary>
    public string KeyName { get; }

    /// <summary>KeyDown/KeyUp icin sanal tus kodu, KeyPress icin karakter kodu.</summary>
    public int KeyCode { get; }

    /// <summary>Olay sirasinda basili olan Ctrl/Shift/Alt bilgisi.</summary>
    public Keys Modifiers { get; }

    private KeyLogEntry(DateTime timestamp, KeyEventType type, string keyName, int keyCode, Keys modifiers)
    {
        if (string.IsNullOrWhiteSpace(keyName))
            throw new ArgumentException("Tus adi bos olamaz.", nameof(keyName));

        Timestamp = timestamp;
        EventType = type;
        KeyName = keyName;
        KeyCode = keyCode;
        Modifiers = modifiers;
    }

    /// <summary>KeyDown / KeyUp olayindan kayit olusturur.</summary>
    public static KeyLogEntry FromKeyEvent(KeyEventType type, KeyEventArgs e, DateTime timestamp)
    {
        if (type == KeyEventType.KeyPress)
            throw new ArgumentException("KeyPress icin FromKeyPress kullanilmalidir.", nameof(type));

        return new KeyLogEntry(timestamp, type, DescribeKey(e.KeyCode), e.KeyValue, e.Modifiers);
    }

    /// <summary>KeyPress olayindan (karakter girisi) kayit olusturur.</summary>
    public static KeyLogEntry FromKeyPress(char keyChar, DateTime timestamp)
    {
        return new KeyLogEntry(timestamp, KeyEventType.KeyPress, DescribeChar(keyChar), keyChar, Control.ModifierKeys);
    }

    /// <summary>Ctrl+Shift gibi kisayol bilgisini metin olarak dondurur.</summary>
    public string ModifierText
    {
        get
        {
            var parts = new List<string>();
            if (Modifiers.HasFlag(Keys.Control)) parts.Add("Ctrl");
            if (Modifiers.HasFlag(Keys.Shift)) parts.Add("Shift");
            if (Modifiers.HasFlag(Keys.Alt)) parts.Add("Alt");
            return string.Join("+", parts);
        }
    }

    /// <summary>Dosyaya ve listeye yazilan standart kayit formati.</summary>
    public override string ToString()
    {
        string time = Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
        string mods = ModifierText.Length > 0 ? $" [{ModifierText}]" : string.Empty;
        return $"{time} | {EventType,-8} | {KeyName}{mods} (kod: {KeyCode})";
    }

    private static string DescribeKey(Keys key) => key switch
    {
        Keys.Return => "Enter",
        Keys.Back => "Backspace",
        Keys.Space => "Space",
        Keys.Escape => "Esc",
        Keys.ControlKey or Keys.LControlKey or Keys.RControlKey => "Ctrl",
        Keys.ShiftKey or Keys.LShiftKey or Keys.RShiftKey => "Shift",
        Keys.Menu or Keys.LMenu or Keys.RMenu => "Alt",
        >= Keys.D0 and <= Keys.D9 => ((int)(key - Keys.D0)).ToString(CultureInfo.InvariantCulture),
        _ => key.ToString()
    };

    private static string DescribeChar(char c) => c switch
    {
        ' ' => "Space",
        '\r' => "Enter",
        '\b' => "Backspace",
        '\t' => "Tab",
        '\u001b' => "Esc",
        _ when char.IsControl(c) => $"Kontrol (0x{(int)c:X2})",
        _ => $"'{c}'"
    };
}
