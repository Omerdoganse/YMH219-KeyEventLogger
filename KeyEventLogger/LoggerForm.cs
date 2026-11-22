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
    private readonly KeyStatistics _statistics = new(topCount: 5);
    private readonly TimeRangeFilter _filter = new();

    // Ust arac cubugu
    private readonly Button _btnStart = new() { Text = "▶ Başlat", Width = 100, Height = 32 };
    private readonly Button _btnStop = new() { Text = "■ Durdur", Width = 100, Height = 32 };
    private readonly Button _btnClear = new() { Text = "Temizle", Width = 100, Height = 32 };
    private readonly Button _btnSave = new() { Text = ".txt Kaydet", Width = 110, Height = 32 };
    private readonly RecordingIndicator _indicator = new();

    // Kayit listesi
    private readonly ListView _list = new()
    {
        View = View.Details,
        FullRowSelect = true,
        GridLines = true,
        HideSelection = false,
        Dock = DockStyle.Fill,
        Font = new Font("Consolas", 10f)
    };

    // Sag panel: olay turu secimi, istatistik, filtre
    private readonly CheckBox _chkDown = new() { Text = "KeyDown", Checked = true, AutoSize = true };
    private readonly CheckBox _chkUp = new() { Text = "KeyUp", Checked = true, AutoSize = true };
    private readonly CheckBox _chkPress = new() { Text = "KeyPress", Checked = true, AutoSize = true };
    private readonly Label _lblStats = new() { AutoSize = false, Dock = DockStyle.Top, Height = 78 };
    private readonly ListBox _lstTopKeys = new() { Dock = DockStyle.Fill, IntegralHeight = false, Font = new Font("Consolas", 10f) };
    private readonly DateTimePicker _dtFrom = CreateTimePicker();
    private readonly DateTimePicker _dtTo = CreateTimePicker();
    private readonly Button _btnFilter = new() { Text = "Filtrele", Width = 100, Height = 28 };
    private readonly Button _btnResetFilter = new() { Text = "Filtreyi Kaldır", Width = 110, Height = 28 };
    private readonly Label _lblFilter = new() { AutoSize = true, ForeColor = Color.DimGray };

    // Alt bolum
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
        ClientSize = new Size(1040, 620);
        MinimumSize = new Size(900, 500);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 9.5f);
        KeyPreview = true;

        // Filtre varsayilan olarak bugunun tamamini kapsar.
        _dtFrom.Value = DateTime.Today;
        _dtTo.Value = DateTime.Today.AddDays(1).AddSeconds(-1);

        BuildLayout();
        WireEvents();
        UpdateRecordingUi();
        UpdateFocusLabel(true);
        RefreshStatistics();
    }

    // ---------------- Arayuz kurulumu ----------------

    private static DateTimePicker CreateTimePicker() => new()
    {
        Format = DateTimePickerFormat.Custom,
        CustomFormat = "dd.MM.yyyy HH:mm:ss",
        Width = 200
    };

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

        var center = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8, 4, 4, 4) };
        center.Controls.Add(_list);

        Controls.Add(center);
        Controls.Add(BuildSidePanel());
        Controls.Add(bottom);
        Controls.Add(toolbar);
        Controls.Add(status);
    }

    private Control BuildSidePanel()
    {
        var side = new Panel { Dock = DockStyle.Right, Width = 250, Padding = new Padding(4, 4, 8, 4) };

        var grpTypes = new GroupBox { Text = "Kaydedilecek olaylar", Dock = DockStyle.Top, Height = 58 };
        var typesFlow = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
        typesFlow.Controls.AddRange(new Control[] { _chkDown, _chkUp, _chkPress });
        grpTypes.Controls.Add(typesFlow);

        var grpFilter = new GroupBox { Text = "Zaman aralığı filtresi", Dock = DockStyle.Bottom, Height = 170 };
        var filterFlow = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false };
        var buttons = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = Padding.Empty };
        buttons.Controls.AddRange(new Control[] { _btnFilter, _btnResetFilter });
        filterFlow.Controls.AddRange(new Control[]
        {
            new Label { Text = "Başlangıç:", AutoSize = true }, _dtFrom,
            new Label { Text = "Bitiş:", AutoSize = true }, _dtTo,
            buttons, _lblFilter
        });
        grpFilter.Controls.Add(filterFlow);

        var grpStats = new GroupBox { Text = "İstatistik", Dock = DockStyle.Fill, Padding = new Padding(6) };
        var topTitle = new Label { Text = "En sık basılan tuşlar (KeyDown):", Dock = DockStyle.Top, Height = 20 };
        grpStats.Controls.Add(_lstTopKeys);
        grpStats.Controls.Add(topTitle);
        grpStats.Controls.Add(_lblStats);

        side.Controls.Add(grpStats);
        side.Controls.Add(grpFilter);
        side.Controls.Add(grpTypes);
        return side;
    }

    private void WireEvents()
    {
        _btnStart.Click += (_, _) => _recorder.Start();
        _btnStop.Click += (_, _) => _recorder.Stop();
        _btnClear.Click += (_, _) => ClearEntries();
        _btnSave.Click += (_, _) => SaveEntries();
        _btnFilter.Click += (_, _) => ApplyFilter();
        _btnResetFilter.Click += (_, _) => ResetFilter();

        _chkDown.CheckedChanged += (_, _) => SetTypeEnabled(KeyEventType.KeyDown, _chkDown.Checked);
        _chkUp.CheckedChanged += (_, _) => SetTypeEnabled(KeyEventType.KeyUp, _chkUp.Checked);
        _chkPress.CheckedChanged += (_, _) => SetTypeEnabled(KeyEventType.KeyPress, _chkPress.Checked);

        KeyDown += (_, e) => HandleKey(KeyEventType.KeyDown, e);
        KeyUp += (_, e) => HandleKey(KeyEventType.KeyUp, e);
        KeyPress += (_, e) => HandleKeyPress(e);

        // Pencere odagini kaybedince kayit alinmadigini acikca goster.
        Activated += (_, _) => UpdateFocusLabel(true);
        Deactivate += (_, _) => UpdateFocusLabel(false);

        _recorder.EntryAdded += (_, entry) => OnEntryAdded(entry);
        _recorder.EntriesCleared += (_, _) => { _list.Items.Clear(); RefreshStatistics(); };
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

    private void SetTypeEnabled(KeyEventType type, bool enabled)
    {
        if (enabled) _recorder.EnabledTypes.Add(type);
        else _recorder.EnabledTypes.Remove(type);
        ShowStatus($"{type} olayları {(enabled ? "kaydedilecek" : "kaydedilmeyecek")}.");
    }

    // ---------------- Liste, istatistik ve durum ----------------

    private void OnEntryAdded(KeyLogEntry entry)
    {
        if (_recorder.Entries.Count >= _recorder.MaxEntries)
        {
            // Bellek sinirina ulasildi: en eski kayit silindigi icin listeyi yeniden kur.
            RebuildList();
        }
        else if (_filter.Matches(entry))
        {
            _list.Items.Add(CreateListItem(entry));
            _list.EnsureVisible(_list.Items.Count - 1);
        }
        RefreshStatistics();
    }

    private void RebuildList()
    {
        _list.BeginUpdate();
        try
        {
            _list.Items.Clear();
            foreach (var entry in _filter.Apply(_recorder.Entries))
                _list.Items.Add(CreateListItem(entry));
            if (_list.Items.Count > 0)
                _list.EnsureVisible(_list.Items.Count - 1);
        }
        finally
        {
            _list.EndUpdate();
        }
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

    private void RefreshStatistics()
    {
        var visible = _filter.Apply(_recorder.Entries).ToList();
        var stats = _statistics.Calculate(visible);

        _lblStats.Text =
            $"Toplam olay: {stats.Total}\n" +
            $"KeyDown: {stats.KeyDownCount}\n" +
            $"KeyUp: {stats.KeyUpCount}\n" +
            $"KeyPress: {stats.KeyPressCount}";

        _lstTopKeys.BeginUpdate();
        _lstTopKeys.Items.Clear();
        int rank = 1;
        foreach (var key in stats.TopKeys)
            _lstTopKeys.Items.Add($"{rank++}. {key.KeyName,-12} {key.Count,4}");
        if (stats.TopKeys.Count == 0)
            _lstTopKeys.Items.Add("(henüz veri yok)");
        _lstTopKeys.EndUpdate();

        _lblCount.Text = _filter.IsActive
            ? $"Gösterilen: {visible.Count} / Toplam: {_recorder.Entries.Count}"
            : $"Kayıt sayısı: {_recorder.Entries.Count}";
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

    private void ShowStatus(string message) => _lblStatus.Text = message;

    // ---------------- Buton islemleri ----------------

    private void ClearEntries()
    {
        _recorder.Clear();
        ShowStatus("Kayıtlar temizlendi.");
        _txtPractice.Clear();
        _txtPractice.Focus();
    }

    private void ApplyFilter()
    {
        try
        {
            _filter.Set(_dtFrom.Value, _dtTo.Value);
            _lblFilter.Text = "Aktif: " + _filter;
            RebuildList();
            RefreshStatistics();
            ShowStatus("Zaman aralığı filtresi uygulandı.");
        }
        catch (ArgumentException ex)
        {
            MessageBox.Show(ex.Message, "Geçersiz aralık", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void ResetFilter()
    {
        _filter.Reset();
        _lblFilter.Text = string.Empty;
        RebuildList();
        RefreshStatistics();
        ShowStatus("Filtre kaldırıldı, tüm kayıtlar gösteriliyor.");
    }

    private void SaveEntries()
    {
        var toSave = _filter.Apply(_recorder.Entries).ToList();
        if (toSave.Count == 0)
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
            _fileService.Save(dialog.FileName, toSave);
            ShowStatus($"{toSave.Count} kayıt kaydedildi: {dialog.FileName}");
        }
        catch (LogSaveException ex)
        {
            MessageBox.Show(ex.Message, "Kaydetme hatası", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
