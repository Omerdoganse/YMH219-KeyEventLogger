using KeyEventLogger.Models;
using KeyEventLogger.Services;

namespace KeyEventLogger;

/// <summary>
/// Ana pencere. KeyPreview acik oldugu icin odaktaki kontrol ne olursa olsun
/// tus olaylari once formun KeyDown/KeyUp/KeyPress olaylarina gelir.
/// Tus olaylari YALNIZCA bu pencere odaktayken yakalanir; global hook kullanilmaz.
/// </summary>
public class LoggerForm : Form
{
    private readonly KeyEventRecorder _recorder = new();
    private readonly LogFileService _fileService = new();

    private readonly Button _btnStart = new() { Text = "▶ Başlat", Width = 100, Height = 32 };
    private readonly Button _btnStop = new() { Text = "■ Durdur", Width = 100, Height = 32 };
    private readonly Button _btnClear = new() { Text = "Temizle", Width = 100, Height = 32 };
    private readonly Button _btnSave = new() { Text = ".txt Kaydet", Width = 110, Height = 32 };
    private readonly RecordingIndicator _indicator = new();

    private readonly ListView _list = new()
    {
        View = View.Details,
        FullRowSelect = true,
        GridLines = true,
        HideSelection = false,
        Dock = DockStyle.Fill,
        Font = new Font("Consolas", 10f)
    };

    private readonly TextBox _txtPractice = new()
    {
        Dock = DockStyle.Fill,
        Font = new Font("Segoe UI", 11f),
        PlaceholderText = "Buraya yazarak deneyin (yalnızca bu pencere odaktayken kayıt alınır)"
    };

    private readonly Label _lblFocus = new()
    {
        AutoSize = false,
        Dock = DockStyle.Right,
        Width = 260,
        TextAlign = ContentAlignment.MiddleCenter,
        Font = new Font("Segoe UI", 9f, FontStyle.Bold)
    };

    private readonly ToolStripStatusLabel _lblStatus = new() { Spring = true, TextAlign = ContentAlignment.MiddleLeft };
    private readonly ToolStripStatusLabel _lblCount = new();

    public LoggerForm()
    {
        Text = "YMH219 - Key Event Logger (Eğitsel)";
        ClientSize = new Size(900, 600);
        MinimumSize = new Size(760, 460);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 9.5f);
        KeyPreview = true;

        BuildLayout();
        WireEvents();
        UpdateRecordingUi();
        UpdateFocusLabel(true);
        UpdateCount();
    }

    // ---------------- Arayuz kurulumu ----------------

    private void BuildLayout()
    {
        var toolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 50,
            Padding = new Padding(8, 8, 8, 0),
            WrapContents = false
        };
        _indicator.Margin = new Padding(16, 0, 0, 0);
        toolbar.Controls.AddRange(new Control[] { _btnStart, _btnStop, _btnClear, _btnSave, _indicator });

        _list.Columns.Add("Zaman", 120);
        _list.Columns.Add("Olay", 90);
        _list.Columns.Add("Tuş", 150);
        _list.Columns.Add("Kod", 60);
        _list.Columns.Add("Değiştirici", 110);

        var bottom = new Panel { Dock = DockStyle.Bottom, Height = 40, Padding = new Padding(8, 6, 8, 6) };
        bottom.Controls.Add(_txtPractice);
        bottom.Controls.Add(_lblFocus);

        var status = new StatusStrip();
        status.Items.Add(_lblStatus);
        status.Items.Add(_lblCount);

        var center = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8, 4, 8, 4) };
        center.Controls.Add(_list);

        Controls.Add(center);
        Controls.Add(bottom);
        Controls.Add(toolbar);
        Controls.Add(status);
    }

    private void WireEvents()
    {
        _btnStart.Click += (_, _) => _recorder.Start();
        _btnStop.Click += (_, _) => _recorder.Stop();
        _btnClear.Click += (_, _) => ClearEntries();
        _btnSave.Click += (_, _) => SaveEntries();

        KeyDown += (_, e) => HandleKey(KeyEventType.KeyDown, e);
        KeyUp += (_, e) => HandleKey(KeyEventType.KeyUp, e);
        KeyPress += (_, e) => HandleKeyPress(e);

        // Pencere odagini kaybedince kayit alinmadigini acikca goster.
        Activated += (_, _) => UpdateFocusLabel(true);
        Deactivate += (_, _) => UpdateFocusLabel(false);

        _recorder.EntryAdded += (_, entry) => AddEntryToList(entry);
        _recorder.EntriesCleared += (_, _) => { _list.Items.Clear(); UpdateCount(); };
        _recorder.RecordingStateChanged += (_, _) => UpdateRecordingUi();
    }

    // ---------------- Klavye olaylari ----------------

    private void HandleKey(KeyEventType type, KeyEventArgs e)
    {
        if (IsPasswordFieldFocused()) return;
        try
        {
            _recorder.Record(KeyLogEntry.FromKeyEvent(type, e, DateTime.Now));
        }
        catch (Exception ex)
        {
            ShowStatus("Olay kaydedilemedi: " + ex.Message);
        }
    }

    private void HandleKeyPress(KeyPressEventArgs e)
    {
        if (IsPasswordFieldFocused()) return;
        try
        {
            _recorder.Record(KeyLogEntry.FromKeyPress(e.KeyChar, DateTime.Now));
        }
        catch (Exception ex)
        {
            ShowStatus("Olay kaydedilemedi: " + ex.Message);
        }
    }

    /// <summary>Parola alanlarina yazilanlar hicbir zaman kaydedilmez.</summary>
    private bool IsPasswordFieldFocused() =>
        ActiveControl is TextBox tb && (tb.UseSystemPasswordChar || tb.PasswordChar != '\0');

    // ---------------- Liste ve durum ----------------

    private void AddEntryToList(KeyLogEntry entry)
    {
        _list.Items.Add(CreateListItem(entry));
        _list.EnsureVisible(_list.Items.Count - 1);

        // Bellek sinirina ulasildiysa listeden de en eski satiri sil.
        while (_list.Items.Count > _recorder.Entries.Count)
            _list.Items.RemoveAt(0);

        UpdateCount();
    }

    private static ListViewItem CreateListItem(KeyLogEntry entry)
    {
        var item = new ListViewItem(entry.Timestamp.ToString("HH:mm:ss.fff"));
        item.SubItems.Add(entry.EventType.ToString());
        item.SubItems.Add(entry.KeyName);
        item.SubItems.Add(entry.KeyCode.ToString());
        item.SubItems.Add(entry.ModifierText);
        item.ForeColor = entry.EventType switch
        {
            KeyEventType.KeyDown => Color.FromArgb(0, 90, 170),
            KeyEventType.KeyUp => Color.FromArgb(110, 110, 110),
            _ => Color.FromArgb(0, 130, 60)
        };
        return item;
    }

    private void UpdateRecordingUi()
    {
        bool rec = _recorder.IsRecording;
        _indicator.IsRecording = rec;
        _btnStart.Enabled = !rec;
        _btnStop.Enabled = rec;
        ShowStatus(rec ? "Kayıt başladı. Bu pencere odaktayken basılan tuşlar listelenir."
                       : "Kayıt durduruldu. Başlamak için Başlat butonuna basın.");
        // Butona tiklandiktan sonra odak deneme alanina gecsin, Space/Enter butonu tetiklemesin.
        _txtPractice.Focus();
    }

    private void UpdateFocusLabel(bool focused)
    {
        _lblFocus.Text = focused ? "Pencere odakta" : "Pencere odakta değil - kayıt alınmıyor";
        _lblFocus.ForeColor = focused ? Color.FromArgb(0, 130, 60) : Color.FromArgb(200, 120, 0);
    }

    private void UpdateCount() => _lblCount.Text = $"Kayıt sayısı: {_recorder.Entries.Count}";

    private void ShowStatus(string message) => _lblStatus.Text = message;

    // ---------------- Buton islemleri ----------------

    private void ClearEntries()
    {
        _recorder.Clear();
        ShowStatus("Kayıtlar temizlendi.");
        _txtPractice.Clear();
        _txtPractice.Focus();
    }

    private void SaveEntries()
    {
        if (_recorder.Entries.Count == 0)
        {
            MessageBox.Show("Kaydedilecek kayıt yok.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var dialog = new SaveFileDialog
        {
            Filter = "Metin dosyası (*.txt)|*.txt",
            FileName = $"tus-kayitlari-{DateTime.Now:yyyyMMdd-HHmmss}.txt",
            Title = "Kayıtları .txt olarak kaydet"
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            _fileService.Save(dialog.FileName, _recorder.Entries);
            ShowStatus($"{_recorder.Entries.Count} kayıt kaydedildi: {dialog.FileName}");
        }
        catch (LogSaveException ex)
        {
            MessageBox.Show(ex.Message, "Kaydetme hatası", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
