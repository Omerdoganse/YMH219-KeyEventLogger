using System.Text;
using KeyEventLogger.Models;

namespace KeyEventLogger.Services;

/// <summary>
/// Kayitlari yerel bir .txt dosyasina yazar. Ag uzerinden hicbir gonderim yapilmaz.
/// </summary>
public class LogFileService
{
    /// <summary>
    /// Kayitlari verilen yola yazar. Hata olursa anlasilir bir mesajla
    /// <see cref="LogSaveException"/> firlatir.
    /// </summary>
    public void Save(string path, IEnumerable<KeyLogEntry> entries)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new LogSaveException("Dosya yolu bos olamaz.");
        ArgumentNullException.ThrowIfNull(entries);

        try
        {
            using var writer = new StreamWriter(path, append: false, Encoding.UTF8);
            writer.WriteLine("# YMH219 Key Event Logger kayitlari");
            writer.WriteLine($"# Olusturulma: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            writer.WriteLine("# Format: zaman | olay | tus (kod)");
            writer.WriteLine();

            int count = 0;
            foreach (var entry in entries)
            {
                writer.WriteLine(entry.ToString());
                count++;
            }

            writer.WriteLine();
            writer.WriteLine($"# Toplam kayit: {count}");
        }
        catch (UnauthorizedAccessException)
        {
            throw new LogSaveException("Bu konuma yazma izniniz yok. Lutfen baska bir klasor secin.");
        }
        catch (DirectoryNotFoundException)
        {
            throw new LogSaveException("Secilen klasor bulunamadi.");
        }
        catch (IOException ex)
        {
            throw new LogSaveException("Dosya yazilamadi (dosya baska bir programda acik olabilir): " + ex.Message);
        }
    }
}

/// <summary>Kayit dosyasi yazilamadiginda kullaniciya gosterilecek hata.</summary>
public class LogSaveException : Exception
{
    public LogSaveException(string message) : base(message) { }
}
